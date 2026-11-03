using DiyVr.Core;
using DiyVr.Stereo;
using DiyVr.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace DiyVr.Passthrough
{
    /// <summary>
    /// Shows the ARCore camera image in both eyes: on a double tap anywhere (routed by GestureArbiter), through
    /// <see cref="Toggle"/>, and automatically while HeadPoseDriver reports tracking lost.
    /// How: an ARCameraBackground is added to the AR camera (only enabled while visible). Each frame its material is
    /// blitted into a RenderTexture, and a quad that fills each eye's frustum, just in front of the eye camera, shows it.
    /// The same mono image goes to both eyes, centre-cropped to the eye aspect. It is NOT scaled to match the lens
    /// field of view, so it does not line up with the real world (open question on the PR).
    /// Added automatically next to StereoRig.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassthroughController : MonoBehaviour
    {
        [Tooltip("Distance of the image quad in front of each eye camera, metres. Keep it above the eye near clip.")]
        public float quadDistance = 0.12f;
        public Vector2Int imageSize = new Vector2Int(1280, 720);
        public DoubleTapDetector tapDetector = new DoubleTapDetector();

        private readonly PassthroughStateMachine _state = new PassthroughStateMachine();
        private StereoRig _rig;
        private HeadPoseDriver _driver;
        private ARCameraBackground _background;
        private RenderTexture _image;
        private Material _material;
        private Camera _leftBuiltFor, _rightBuiltFor;
        private Matrix4x4 _leftProj, _rightProj;
        private GameObject _leftQuad, _rightQuad;
        private Mesh _leftMesh, _rightMesh;
        private bool _wasVisible;

        public PassthroughMode Mode { get { return _state.Mode; } }
        public bool IsVisible { get { return _state.IsVisible; } }
        /// <summary>Raised when the passthrough view is shown (true) or hidden (false).</summary>
        public event System.Action<bool> VisibilityChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var rig = FindFirstObjectByType<StereoRig>();
            if (rig != null && rig.GetComponent<PassthroughController>() == null)
                rig.gameObject.AddComponent<PassthroughController>();
        }

        /// <summary>Turns passthrough on or off by hand (same as a double tap).</summary>
        public void Toggle() { _state.Toggle(); }

        /// <summary>Sets the manual state. Automatic display on tracking loss still applies.</summary>
        public void SetManual(bool on) { _state.SetManual(on); }

        private void OnEnable()
        {
            _rig = GetComponent<StereoRig>();
            _wasVisible = false;
        }

        private void OnDisable()
        {
            if (_driver != null) _driver.TrackingChanged -= OnTrackingChanged;
            _driver = null;
            HideVisuals();
            DestroyVisuals();
        }

        private void OnTrackingChanged(bool tracking) { _state.SetTracking(tracking); }

        private void Update()
        {
            if (_driver == null)
            {
                _driver = GetComponent<HeadPoseDriver>();
                if (_driver == null) _driver = FindFirstObjectByType<HeadPoseDriver>();
                if (_driver != null)
                {
                    _driver.TrackingChanged += OnTrackingChanged;
                    _state.SetTracking(_driver.IsTracking);
                }
            }
            ReadTaps();
        }

        private void ReadTaps()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;
            int down = 0;
            for (int i = 0; i < ts.touches.Count; i++) if (ts.touches[i].press.isPressed) down++;
            if (down > 1 || GestureArbiter.Shared.LensToolOpen) { tapDetector.Cancel(); return; }

            var touch = ts.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                tapDetector.OnPress(Time.unscaledTimeAsDouble, touch.position.ReadValue());
            if (touch.press.wasReleasedThisFrame
                && tapDetector.OnRelease(Time.unscaledTimeAsDouble, touch.position.ReadValue())
                && GestureArbiter.Shared.Allows(GestureTarget.Passthrough, TouchGesture.DoubleTap))
                _state.Toggle();
        }

        private void LateUpdate()
        {
            bool visible = _state.IsVisible;
            if (visible != _wasVisible)
            {
                _wasVisible = visible;
                if (!visible) HideVisuals();
                var h = VisibilityChanged;
                if (h != null) h(visible);
            }
            if (!visible) return;
            if (!UpdateImage()) return;
            UpdateQuads();
        }

        // Returns true once there is an image to show.
        private bool UpdateImage()
        {
            if (_driver == null || _driver.ArCamera == null) return false;
            if (_background == null)
            {
                _background = _driver.ArCamera.GetComponent<ARCameraBackground>();
                if (_background == null) _background = _driver.ArCamera.gameObject.AddComponent<ARCameraBackground>();
            }
            if (!_background.enabled) _background.enabled = true;
            Material m = _background.material;
            if (m == null) return false; // created by ARCameraBackground when it starts
            if (_image == null)
            {
                _image = new RenderTexture(imageSize.x, imageSize.y, 0, RenderTextureFormat.Default) { name = "PassthroughImage" };
                _image.wrapMode = TextureWrapMode.Clamp;
                _image.filterMode = FilterMode.Bilinear;
                _image.Create();
            }
            Graphics.Blit(null, _image, m);
            return true;
        }

        private void UpdateQuads()
        {
            if (_rig == null || _rig.LeftEye == null || _rig.RightEye == null) return;
            if (_material == null)
            {
                Shader shader = _rig.unlitShader != null ? _rig.unlitShader : Shader.Find("DiyVr/StereoUnlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) return;
                _material = new Material(shader) { name = "PassthroughMat" };
            }
            _material.mainTexture = _image;
            Ensure(_rig.LeftEye, true, ref _leftBuiltFor, ref _leftProj, ref _leftQuad, ref _leftMesh, "PassthroughLeft");
            Ensure(_rig.RightEye, false, ref _rightBuiltFor, ref _rightProj, ref _rightQuad, ref _rightMesh, "PassthroughRight");
            if (_leftQuad != null) _leftQuad.SetActive(true);
            if (_rightQuad != null) _rightQuad.SetActive(true);
        }

        // (Re)builds the quad when the eye camera was replaced (StereoRig.Rebuild) or its projection changed.
        private void Ensure(Camera eye, bool left, ref Camera builtFor, ref Matrix4x4 proj, ref GameObject quad, ref Mesh mesh, string name)
        {
            if (quad != null && builtFor == eye && proj == eye.projectionMatrix) return;
            if (quad != null) Destroy(quad);
            if (mesh != null) Destroy(mesh);

            Vector3[] c = PassthroughLayout.FrustumQuad(eye.projectionMatrix, quadDistance);
            float aspect = (c[1] - c[0]).magnitude / Mathf.Max(1e-6f, (c[3] - c[0]).magnitude);
            Rect uv = PassthroughLayout.CenterCrop(aspect, (float)imageSize.x / imageSize.y);

            mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = c;
            mesh.uv = new[]
            {
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin),
                new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();

            quad = new GameObject(name) { layer = left ? PassthroughLayout.LeftLayer : PassthroughLayout.RightLayer };
            // Each eye must not see the other eye's quad (that was the doubled image in the right eye).
            eye.cullingMask = PassthroughLayout.EyeCullingMask(eye.cullingMask, left);
            quad.transform.SetParent(eye.transform, false);
            quad.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = quad.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            builtFor = eye;
            proj = eye.projectionMatrix;
        }

        private void HideVisuals()
        {
            if (_leftQuad != null) _leftQuad.SetActive(false);
            if (_rightQuad != null) _rightQuad.SetActive(false);
            if (_background != null) _background.enabled = false; // stop the per-frame camera texture work
        }

        private void DestroyVisuals()
        {
            if (_leftQuad != null) Destroy(_leftQuad);
            if (_rightQuad != null) Destroy(_rightQuad);
            if (_leftMesh != null) Destroy(_leftMesh);
            if (_rightMesh != null) Destroy(_rightMesh);
            if (_material != null) Destroy(_material);
            if (_image != null) Destroy(_image);
            _leftQuad = _rightQuad = null; _leftMesh = _rightMesh = null; _material = null; _image = null;
            _leftBuiltFor = _rightBuiltFor = null;
        }
    }
}
