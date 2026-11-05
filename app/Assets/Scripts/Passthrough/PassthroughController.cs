using DiyVr.Core;
using DiyVr.Stereo;
using DiyVr.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Passthrough
{
    /// <summary>
    /// Shows the ARCore camera image in both eyes: on a double tap anywhere (routed by GestureArbiter), through
    /// <see cref="Toggle"/>, and automatically while HeadPoseDriver reports tracking lost.
    /// How: an ARCameraBackground is added to the AR camera (only enabled while visible). Each frame its material is
    /// blitted into a RenderTexture, and a quad just in front of each eye camera shows it. The quad spans exactly the
    /// camera's real field of view (from the ARCore intrinsics) and is centred straight ahead of the eye, so the
    /// image has its true angular size. Its material draws as background and writes no depth, so scene content and
    /// the lens tool panel draw on top. The same mono image goes to both eyes.
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
        private int _leftBuiltVersion = -1, _rightBuiltVersion = -1;
        private ARCameraManager _cameraManager;
        private Vector3[] _corners;
        private int _cornersVersion;
        private Vector4 _intrinsicsKey;
        private float _screenAspectKey;
        private bool _hasIntrinsics;
        private GameObject _leftQuad, _rightQuad;
        private Mesh _leftMesh, _rightMesh;
        private bool _wasVisible;
        private bool _askedAtFirstTracking;
        private Coroutine _lateRequest;

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
            _state.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            _state.ModeChanged -= OnModeChanged;
            _lateRequest = null;
            if (_driver != null) _driver.TrackingChanged -= OnTrackingChanged;
            _driver = null;
            HideVisuals();
            DestroyVisuals();
        }

        private void OnTrackingChanged(bool tracking)
        {
            _state.SetTracking(tracking);
            AskOnFirstTracking(tracking);
        }

        // Camera passthrough makes Android drop the display to 60 Hz; ask for the best mode again once the view is hidden.
        private void OnModeChanged(PassthroughMode mode)
        {
            if (mode != PassthroughMode.Off) return;
            AppBootstrap.RequestHighestRefreshRate();
            // The camera background may only be torn down a frame later, so ask once more after a second.
            if (_lateRequest != null) StopCoroutine(_lateRequest);
            _lateRequest = StartCoroutine(RequestAgainLater());
        }

        private System.Collections.IEnumerator RequestAgainLater()
        {
            yield return new WaitForSecondsRealtime(1f);
            _lateRequest = null;
            if (!_state.IsVisible) AppBootstrap.RequestHighestRefreshRate();
        }

        private void AskOnFirstTracking(bool tracking)
        {
            if (!tracking || _askedAtFirstTracking) return;
            _askedAtFirstTracking = true;
            AppBootstrap.RequestHighestRefreshRate();
        }

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
                    AskOnFirstTracking(_driver.IsTracking);
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
                // Background shader without depth write; the fallbacks write depth but still draw first.
                Shader shader = Shader.Find("DiyVr/PassthroughImage");
                if (shader == null) shader = _rig.unlitShader != null ? _rig.unlitShader : Shader.Find("DiyVr/StereoUnlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) return;
                _material = new Material(shader) { name = "PassthroughMat" };
                _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;
            }
            _material.mainTexture = _image;
            UpdateCorners();
            Ensure(_rig.LeftEye, true, ref _leftBuiltFor, ref _leftBuiltVersion, ref _leftQuad, ref _leftMesh, "PassthroughLeft");
            Ensure(_rig.RightEye, false, ref _rightBuiltFor, ref _rightBuiltVersion, ref _rightQuad, ref _rightMesh, "PassthroughRight");
            if (_leftQuad != null) _leftQuad.SetActive(true);
            if (_rightQuad != null) _rightQuad.SetActive(true);
        }

        // (Re)builds the quad when the eye camera was replaced (StereoRig.Rebuild) or its projection changed.
        private void Ensure(Camera eye, bool left, ref Camera builtFor, ref int builtVersion, ref GameObject quad, ref Mesh mesh, string name)
        {
            if (quad != null && builtFor == eye && builtVersion == _cornersVersion) return;
            if (quad != null) Destroy(quad);
            if (mesh != null) Destroy(mesh);

            // The image fills the whole quad: the blit already shows the part of the camera image that matches the screen.
            mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = _corners;
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
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
            builtVersion = _cornersVersion;
        }

        // Quad corners from the camera intrinsics (true angular size, centred on the optical axis). The blit through
        // ARCameraBackground shows the centre crop of the camera image that has the screen's aspect, so that crop is
        // what the quad spans. Falls back to a symmetric 70 x 55 degree quad until intrinsics are available.
        private void UpdateCorners()
        {
            float screenAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            Vector4 key = Vector4.zero;
            bool have = false;
            if (_cameraManager == null && _driver != null && _driver.ArCamera != null)
                _cameraManager = _driver.ArCamera.GetComponent<ARCameraManager>();
            if (_cameraManager == null) _cameraManager = FindFirstObjectByType<ARCameraManager>();
            XRCameraIntrinsics k;
            if (_cameraManager != null && _cameraManager.TryGetIntrinsics(out k)
                && k.resolution.x > 0 && k.resolution.y > 0 && k.focalLength.x > 0f && k.focalLength.y > 0f)
            {
                have = true;
                key = new Vector4(k.focalLength.x, k.focalLength.y, k.principalPoint.x, k.principalPoint.y);
                key.x += k.resolution.x * 1e6f; // fold the resolution into the change check
                key.y += k.resolution.y * 1e6f;
                if (_corners != null && _hasIntrinsics && key == _intrinsicsKey && Mathf.Approximately(screenAspect, _screenAspectKey)) return;
                float w = k.resolution.x, h = k.resolution.y;
                Rect visible = PassthroughLayout.CenterCrop(screenAspect, w / h);
                _corners = PassthroughLayout.IntrinsicsQuad(k.focalLength.x, k.focalLength.y, k.principalPoint.x, k.principalPoint.y,
                    w, h, quadDistance, visible);
            }
            else
            {
                if (_corners != null && !_hasIntrinsics) return;
                _corners = PassthroughLayout.SymmetricQuad(PassthroughLayout.FallbackHorizontalFovDegrees,
                    PassthroughLayout.FallbackVerticalFovDegrees, quadDistance);
            }
            _hasIntrinsics = have;
            _intrinsicsKey = key;
            _screenAspectKey = screenAspect;
            _cornersVersion++;
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
            _leftBuiltVersion = _rightBuiltVersion = -1;
        }
    }
}
