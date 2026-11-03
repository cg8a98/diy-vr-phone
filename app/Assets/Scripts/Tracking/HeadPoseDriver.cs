using DiyVr.Stereo;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Tracking
{
    /// <summary>
    /// Drives the StereoRig head transform from the ARCore camera pose.
    /// Creates (at runtime, so no scene YAML) an AR Session + XR Origin + AR camera. The AR camera is only a pose
    /// source: it draws nothing to the screen (empty culling mask, tiny off-screen target). The pose is read in
    /// LateUpdate, smoothed and predicted by <see cref="HeadPoseTracker"/>, then written to this transform.
    /// Put it on the same object as StereoRig, or let <see cref="Bootstrap"/> add it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeadPoseDriver : MonoBehaviour
    {
        public HeadPoseSettings settings = new HeadPoseSettings();
        [Tooltip("Ask ARCore to keep focus fixed. Exposure cannot be locked through AR Foundation.")]
        public bool lockFocus = true;

        private HeadPoseTracker _tracker;
        private GameObject _arRoot;
        private Camera _arCamera;
        private RenderTexture _arDummyRt;
        private ARCameraManager _cameraManager;

        /// <summary>Tracker access for the debug overlay (1.7) and passthrough (1.8).</summary>
        public HeadPoseTracker Tracker { get { return _tracker; } }
        /// <summary>True while ARCore tracks well. False means the pose is being held.</summary>
        public bool IsTracking { get { return _tracker != null && _tracker.IsTracking; } }
        /// <summary>Raised on loss (false) and recovery (true).</summary>
        public event System.Action<bool> TrackingChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var rig = FindFirstObjectByType<StereoRig>();
            if (rig != null && rig.GetComponent<HeadPoseDriver>() == null) rig.gameObject.AddComponent<HeadPoseDriver>();
        }

        private void OnEnable()
        {
            _tracker = new HeadPoseTracker(settings);
            _tracker.TrackingChanged += OnTrackingChanged;
            BuildArObjects();
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.TrackingChanged -= OnTrackingChanged;
            if (_arRoot != null) Destroy(_arRoot);
            if (_arDummyRt != null) Destroy(_arDummyRt);
            _arRoot = null; _arCamera = null; _arDummyRt = null; _cameraManager = null;
        }

        private void OnTrackingChanged(bool tracking)
        {
            var handler = TrackingChanged;
            if (handler != null) handler(tracking);
        }

        private void BuildArObjects()
        {
            // Inactive while assembling so components see their final settings in OnEnable.
            _arRoot = new GameObject("AR Rig");
            _arRoot.SetActive(false);
            _arRoot.AddComponent<ARSession>();
            _arRoot.AddComponent<ARInputManager>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(_arRoot.transform, false);

            var camGo = new GameObject("AR Camera");
            camGo.transform.SetParent(offset.transform, false);
            _arCamera = camGo.AddComponent<Camera>();
            _arCamera.cullingMask = 0;
            _arCamera.clearFlags = CameraClearFlags.Nothing;
            _arCamera.depth = -100;
            _arCamera.allowMSAA = false;
            _arCamera.nearClipPlane = 0.1f;
            _arCamera.farClipPlane = 10f;
            // Renders nothing, and into a 16x16 texture so nothing can reach the screen.
            _arDummyRt = new RenderTexture(16, 16, 0) { name = "ArPoseDummyRT" };
            _arCamera.targetTexture = _arDummyRt;

            _cameraManager = camGo.AddComponent<ARCameraManager>();
            _cameraManager.autoFocusRequested = !lockFocus;
            _cameraManager.requestedLightEstimation = LightEstimation.None;

            var driver = camGo.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.positionInput = new InputActionProperty(NewAction("pos", "Vector3",
                "<HandheldARInputDevice>/devicePosition", "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new InputActionProperty(NewAction("rot", "Quaternion",
                "<HandheldARInputDevice>/deviceRotation", "<XRHMD>/centerEyeRotation"));
            driver.trackingStateInput = new InputActionProperty(NewAction("state", "Integer",
                "<HandheldARInputDevice>/trackingState", "<XRHMD>/trackingState"));

            var origin = _arRoot.AddComponent<XROrigin>();
            origin.Origin = _arRoot;
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = _arCamera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

            _arRoot.SetActive(true);
        }

        private static InputAction NewAction(string name, string type, string binding1, string binding2)
        {
            var a = new InputAction(name, InputActionType.Value, binding1, expectedControlType: type);
            a.AddBinding(binding2);
            a.Enable();
            return a;
        }

        private static bool ArTracking()
        {
            return ARSession.state == ARSessionState.SessionTracking
                && ARSession.notTrackingReason == NotTrackingReason.None;
        }

        private void LateUpdate()
        {
            if (_tracker == null || _arCamera == null) return;
            Transform t = _arCamera.transform;
            _tracker.Update(Time.unscaledTimeAsDouble, t.position, t.rotation, ArTracking());
            if (_tracker.HasPose) transform.SetPositionAndRotation(_tracker.Position, _tracker.Rotation);
        }
    }
}
