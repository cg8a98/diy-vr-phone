using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// Draws one hand: 21 small spheres and 21 bone lines in world space on the default layer (both eye cameras see it).
    /// Gets a new pose about 15 times a second; every frame the drawn points chase the newest target so the motion
    /// looks continuous. Hidden when no result has included this hand for <see cref="HideAfterSeconds"/>.
    /// Positions only live in memory; nothing is saved or sent.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        public const float HideAfterSeconds = 0.2f;
        public const float JointDiameter = 0.008f;
        public const float BoneWidth = 0.004f;
        private const float ChaseSeconds = 0.04f;

        private readonly Vector3[] _target = new Vector3[HandPose.LandmarkCount];
        private readonly Vector3[] _shown = new Vector3[HandPose.LandmarkCount];
        private readonly HandOneEuro _depth = new HandOneEuro(1.0f, 1.5f, 1f);
        private Transform[] _joints;
        private LineRenderer[] _bones;
        private Material _material;
        private float _lastSeen = float.NegativeInfinity;
        private bool _visible;

        public void Build(Color colour)
        {
            Shader shader = Shader.Find("DiyVr/StereoUnlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _material = new Material(shader) { name = "HandMaterial" };
            _material.color = colour;

            _joints = new Transform[HandPose.LandmarkCount];
            for (int i = 0; i < _joints.Length; i++)
            {
                GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Joint" + i;
                Destroy(s.GetComponent<Collider>());
                s.transform.SetParent(transform, false);
                s.transform.localScale = Vector3.one * JointDiameter;
                s.GetComponent<MeshRenderer>().sharedMaterial = _material;
                _joints[i] = s.transform;
            }

            _bones = new LineRenderer[HandSkeleton.BoneCount];
            for (int i = 0; i < _bones.Length; i++)
            {
                var go = new GameObject("Bone" + i);
                go.transform.SetParent(transform, false);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = lr.endWidth = BoneWidth;
                lr.sharedMaterial = _material;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                _bones[i] = lr;
            }
            SetVisible(false);
        }

        /// <summary>Call when a new result contains this hand.</summary>
        public void Apply(HandPose pose, HandFrameInfo frame, float now)
        {
            if (pose == null || pose.Landmarks == null || pose.Landmarks.Length < HandPose.LandmarkCount) return;
            if (!frame.Intrinsics.IsValid) return;

            if (!_visible || now - _lastSeen > HideAfterSeconds) _depth.Reset();
            float depth = _depth.Filter(HandProjection.EstimateDepth(pose.Landmarks, frame.Intrinsics), now);
            Vector3[] local = HandProjection.Project(pose.Landmarks, frame.Intrinsics, depth);
            for (int i = 0; i < _target.Length; i++) _target[i] = frame.ToWorld(local[i]);

            if (!_visible || now - _lastSeen > HideAfterSeconds)
                System.Array.Copy(_target, _shown, _shown.Length); // reappearing: jump, do not fly in
            _lastSeen = now;
            SetVisible(true);
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            float now = Time.unscaledTime;
            if (now - _lastSeen > HideAfterSeconds)
            {
                SetVisible(false);
                return;
            }

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < _shown.Length; i++)
            {
                _shown[i] = HandChase.Step(_shown[i], _target[i], dt, ChaseSeconds);
                _joints[i].position = _shown[i];
            }
            for (int b = 0; b < _bones.Length; b++)
            {
                _bones[b].SetPosition(0, _shown[HandSkeleton.Bones[b, 0]]);
                _bones[b].SetPosition(1, _shown[HandSkeleton.Bones[b, 1]]);
            }
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            if (_joints == null) return;
            for (int i = 0; i < _joints.Length; i++) _joints[i].gameObject.SetActive(visible);
            for (int i = 0; i < _bones.Length; i++) _bones[i].gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
