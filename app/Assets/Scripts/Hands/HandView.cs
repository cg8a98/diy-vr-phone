using DiyVr.Passthrough;
using DiyVr.Stereo;
using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// Draws one hand: 21 small spheres and 21 bone lines.
    /// Camera view ON: the skeleton is drawn on the passthrough image plane of each eye (own copy per eye, on that
    /// eye's layer), at the pixel where the camera saw each landmark. The camera image is mono and has no disparity,
    /// so a skeleton with the same (zero) disparity sits exactly on the hand and is as sharp as the image.
    /// Camera view OFF: the world-space 3D skeleton (default layer, both eyes, real depth).
    /// The 3D positions are still computed either way (HandProjection) for pinch, gaze and placement.
    /// Gets a new pose 20-30 times a second; every frame the drawn points chase the newest target, nudged ahead
    /// along the last velocity by part of the result's measured age, so the motion looks continuous and lags less.
    /// Hidden when no result has included this hand for <see cref="HideAfterSeconds"/>.
    /// Positions only live in memory; nothing is saved or sent.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        public const float HideAfterSeconds = 0.2f;
        public const float JointDiameter = 0.008f;
        public const float BoneWidth = 0.004f;
        private const float ChaseSeconds = 0.015f;

        private sealed class EyeSkeleton
        {
            public Camera Eye;
            public GameObject Root;
            public Transform[] Joints;
            public LineRenderer[] Bones;
        }

        private readonly Vector3[] _target = new Vector3[HandPose.LandmarkCount];
        private readonly Vector3[] _prevTarget = new Vector3[HandPose.LandmarkCount];
        private readonly Vector3[] _shown = new Vector3[HandPose.LandmarkCount];
        // Normalised image positions (u, v, 0) for the passthrough skeleton.
        private readonly Vector3[] _uv = new Vector3[HandPose.LandmarkCount];
        private readonly Vector3[] _prevUv = new Vector3[HandPose.LandmarkCount];
        private readonly Vector3[] _uvShown = new Vector3[HandPose.LandmarkCount];
        private readonly HandOneEuro _depth = new HandOneEuro(1.0f, 1.5f, 1f);
        private readonly EyeSkeleton[] _eyes = new EyeSkeleton[2];
        private Transform[] _joints;
        private LineRenderer[] _bones;
        private Material _material;
        private StereoRig _rig;
        private PassthroughController _passthrough;
        private HandIntrinsics _k;
        private float _lastSeen = float.NegativeInfinity;
        private float _interval; // seconds between the camera frames behind the last two results
        private double _capture; // HandClock time the newest result's camera frame was exposed
        private bool _visible;
        private bool _hasPrev;
        private bool _worldShown, _passShown;

        public void Build(Color colour)
        {
            Shader shader = Shader.Find("DiyVr/StereoUnlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _material = new Material(shader) { name = "HandMaterial" };
            _material.color = colour;

            _joints = new Transform[HandPose.LandmarkCount];
            for (int i = 0; i < _joints.Length; i++)
                _joints[i] = NewJoint(transform, "Joint" + i, JointDiameter, 0);

            _bones = new LineRenderer[HandSkeleton.BoneCount];
            for (int i = 0; i < _bones.Length; i++)
                _bones[i] = NewBone(transform, "Bone" + i, BoneWidth, true, 0);
            _worldShown = true; // force the first SetWorldShown(false) to hide them
            SetWorldShown(false);
        }

        private Transform NewJoint(Transform parent, string name, float diameter, int layer)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            Destroy(s.GetComponent<Collider>());
            s.layer = layer;
            s.transform.SetParent(parent, false);
            s.transform.localScale = Vector3.one * diameter;
            var mr = s.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return s.transform;
        }

        private LineRenderer NewBone(Transform parent, string name, float width, bool worldSpace, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = worldSpace;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = width;
            lr.sharedMaterial = _material;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        /// <summary>Call when a new result contains this hand.</summary>
        public void Apply(HandPose pose, HandFrameInfo frame, float now)
        {
            if (pose == null || pose.Landmarks == null || pose.Landmarks.Length < HandPose.LandmarkCount) return;
            if (!frame.Intrinsics.IsValid) return;

            bool continuing = _visible && now - _lastSeen <= HideAfterSeconds;
            if (!continuing) _depth.Reset();
            float depth = _depth.Filter(HandProjection.EstimateDepth(pose.Landmarks, frame.Intrinsics), now);
            Vector3[] local = HandProjection.Project(pose.Landmarks, frame.Intrinsics, depth);

            // Keep the previous result as the velocity reference (only meaningful when it is recent).
            System.Array.Copy(_target, _prevTarget, _target.Length);
            System.Array.Copy(_uv, _prevUv, _uv.Length);
            _interval = continuing ? (float)(frame.CaptureSeconds - _capture) : 0f;
            _hasPrev = continuing;
            _capture = frame.CaptureSeconds;

            _k = frame.Intrinsics;
            for (int i = 0; i < _target.Length; i++)
            {
                _target[i] = frame.ToWorld(local[i]);
                _uv[i] = new Vector3(pose.Landmarks[i].x, pose.Landmarks[i].y, 0f);
            }

            if (!continuing)
            {
                System.Array.Copy(_target, _shown, _shown.Length); // reappearing: jump, do not fly in
                System.Array.Copy(_uv, _uvShown, _uvShown.Length);
            }
            _lastSeen = now;
            _visible = true;
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            float now = Time.unscaledTime;
            if (now - _lastSeen > HideAfterSeconds)
            {
                _visible = false;
                SetWorldShown(false);
                SetPassShown(false);
                return;
            }

            if (_passthrough == null) _passthrough = FindFirstObjectByType<PassthroughController>();
            if (_rig == null) _rig = FindFirstObjectByType<StereoRig>();
            bool onImage = _passthrough != null && _passthrough.IsVisible && _rig != null
                && _rig.LeftEye != null && _rig.RightEye != null;

            float dt = Time.unscaledDeltaTime;
            // The result describes the camera frame from ageNow seconds ago; look ahead along the velocity by a
            // modest share of that (HandLatency.LeadSeconds), never more than one frame interval (Predict).
            float since = HandLatency.LeadSeconds(HandClock.Now() - _capture);
            float interval = _hasPrev ? _interval : 0f;

            // The 3D chase always runs (cheap) so the world points are right the moment the camera view turns off.
            for (int i = 0; i < _shown.Length; i++)
            {
                Vector3 t = HandExtrapolator.Predict(_prevTarget[i], _target[i], interval, since);
                _shown[i] = HandChase.Step(_shown[i], t, dt, ChaseSeconds);
            }

            if (onImage)
            {
                SetWorldShown(false);
                for (int i = 0; i < _uvShown.Length; i++)
                {
                    Vector3 t = HandExtrapolator.Predict(_prevUv[i], _uv[i], interval, since);
                    _uvShown[i] = HandChase.Step(_uvShown[i], t, dt, ChaseSeconds);
                }
                DrawOnImage(0, _rig.LeftEye, PassthroughLayout.LeftLayer, true);
                DrawOnImage(1, _rig.RightEye, PassthroughLayout.RightLayer, false);
                SetPassShown(true);
            }
            else
            {
                SetPassShown(false);
                SetWorldShown(true);
                for (int i = 0; i < _shown.Length; i++) _joints[i].position = _shown[i];
                for (int b = 0; b < _bones.Length; b++)
                {
                    _bones[b].SetPosition(0, _shown[HandSkeleton.Bones[b, 0]]);
                    _bones[b].SetPosition(1, _shown[HandSkeleton.Bones[b, 1]]);
                }
            }
        }

        // Same local position in both eyes (mono image): zero disparity, like the camera image itself.
        private void DrawOnImage(int slot, Camera eye, int layer, bool left)
        {
            EyeSkeleton e = _eyes[slot];
            if (e == null || e.Eye != eye || e.Root == null)
            {
                if (e != null && e.Root != null) Destroy(e.Root);
                e = BuildEye(eye, layer, left);
                _eyes[slot] = e;
            }
            for (int i = 0; i < e.Joints.Length; i++)
                e.Joints[i].localPosition = HandPassthroughMap.PlanePoint(_uvShown[i].x, _uvShown[i].y, _k, HandPassthroughMap.SkeletonDistance);
            for (int b = 0; b < e.Bones.Length; b++)
            {
                e.Bones[b].SetPosition(0, e.Joints[HandSkeleton.Bones[b, 0]].localPosition);
                e.Bones[b].SetPosition(1, e.Joints[HandSkeleton.Bones[b, 1]].localPosition);
            }
        }

        private EyeSkeleton BuildEye(Camera eye, int layer, bool left)
        {
            // Each eye sees its own layer only (PassthroughController does the same for its image quad).
            eye.cullingMask = PassthroughLayout.EyeCullingMask(eye.cullingMask, left);
            var e = new EyeSkeleton { Eye = eye, Root = new GameObject(left ? "HandOnImageLeft" : "HandOnImageRight") { layer = layer } };
            e.Root.transform.SetParent(eye.transform, false);
            float joint = HandPassthroughMap.ScaledSize(JointDiameter);
            float bone = HandPassthroughMap.ScaledSize(BoneWidth);
            e.Joints = new Transform[HandPose.LandmarkCount];
            for (int i = 0; i < e.Joints.Length; i++) e.Joints[i] = NewJoint(e.Root.transform, "Joint" + i, joint, layer);
            e.Bones = new LineRenderer[HandSkeleton.BoneCount];
            for (int i = 0; i < e.Bones.Length; i++) e.Bones[i] = NewBone(e.Root.transform, "Bone" + i, bone, false, layer);
            e.Root.SetActive(_passShown);
            return e;
        }

        private void SetWorldShown(bool shown)
        {
            if (_joints == null || _worldShown == shown) return;
            _worldShown = shown;
            for (int i = 0; i < _joints.Length; i++) _joints[i].gameObject.SetActive(shown);
            for (int i = 0; i < _bones.Length; i++) _bones[i].gameObject.SetActive(shown);
        }

        private void SetPassShown(bool shown)
        {
            if (_passShown == shown) return;
            _passShown = shown;
            for (int i = 0; i < _eyes.Length; i++)
                if (_eyes[i] != null && _eyes[i].Root != null) _eyes[i].Root.SetActive(shown);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _eyes.Length; i++)
                if (_eyes[i] != null && _eyes[i].Root != null) Destroy(_eyes[i].Root);
            if (_material != null) Destroy(_material);
        }
    }
}
