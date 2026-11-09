using System.Collections.Generic;
using DiyVr.Stereo;
using Unity.Collections;
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
        private readonly TrackingHistory _history = new TrackingHistory();
        private GameObject _arRoot;
        private Camera _arCamera;
        private RenderTexture _arDummyRt;
        private ARCameraManager _cameraManager;
        private GameObject _planeTemplate;
        private bool _configDone;
        private float _nextConfigTry;

        /// <summary>Frame rate of the camera configuration that was selected (0 until chosen or if unknown).</summary>
        public int ChosenCameraFps { get; private set; }

        /// <summary>Tracker access for the debug overlay (1.7) and passthrough (1.8).</summary>
        public HeadPoseTracker Tracker { get { return _tracker; } }
        /// <summary>Tracking-loss log for the debug overlay (1.20). Diagnostic only; never changes the pose.</summary>
        public TrackingHistory History { get { return _history; } }
        /// <summary>The AR camera (pose source). Null until enabled. Passthrough (1.8) attaches the camera background to it.</summary>
        public Camera ArCamera { get { return _arCamera; } }
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
            if (_planeTemplate != null) Destroy(_planeTemplate);
            _planeTemplate = null;
            _arRoot = null; _arCamera = null; _arDummyRt = null; _cameraManager = null;
            _configDone = false; ChosenCameraFps = 0;
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
            var session = _arRoot.AddComponent<ARSession>();
            // Default is true: Unity would then cap the app at the 30 fps camera rate. Set before activation.
            session.matchFrameRateRequested = false;
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

            // Horizontal plane detection (desk, floor). Visual only: nothing here reads planes into the head pose.
            _planeTemplate = PlanePrefabFactory.Create();
            _planeTemplate.transform.SetParent(transform, false);
            var planeManager = _arRoot.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            planeManager.planePrefab = _planeTemplate;
            _arRoot.AddComponent<PlaneDebugView>().planeManager = planeManager;

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

        // The reason tracking is not good: ARCore's notTrackingReason, or the session state when that reads None.
        private static string NotTrackingReasonLabel()
        {
            NotTrackingReason reason = ARSession.notTrackingReason;
            if (reason != NotTrackingReason.None) return reason.ToString();
            return ARSession.state.ToString();
        }

        // Switches to a higher-frame-rate camera configuration only if one exists; otherwise keeps the default, once the camera subsystem reports any.
        private void TrySelectCameraConfiguration()
        {
            if (_configDone || _cameraManager == null || Time.unscaledTime < _nextConfigTry) return;
            _nextConfigTry = Time.unscaledTime + 0.5f;
            if (_cameraManager.subsystem == null) return;
            NativeArray<XRCameraConfiguration> configs = default(NativeArray<XRCameraConfiguration>);
            try
            {
                configs = _cameraManager.GetConfigurations(Allocator.Temp);
                {
                    if (!configs.IsCreated || configs.Length == 0) return;
                    var infos = new List<CameraConfigInfo>(configs.Length);
                    for (int i = 0; i < configs.Length; i++)
                    {
                        object fps = configs[i].framerate; // int? in AR Foundation; boxing keeps this compile-safe
                        infos.Add(new CameraConfigInfo(configs[i].width, configs[i].height, fps is int ? (int)fps : 0));
                    }
                    int best = CameraConfigChooser.ChooseIndex(infos);
                    _configDone = true;
                    if (best < 0) return;
                    var current = _cameraManager.currentConfiguration;
                    bool hasCurrent = current.HasValue;
                    CameraConfigInfo currentInfo = default(CameraConfigInfo);
                    if (hasCurrent)
                    {
                        object curFps = current.Value.framerate;
                        currentInfo = new CameraConfigInfo(current.Value.width, current.Value.height, curFps is int ? (int)curFps : 0);
                    }
                    if (CameraConfigChooser.ShouldSwitch(currentInfo, hasCurrent, infos[best]))
                    {
                        _cameraManager.currentConfiguration = configs[best];
                        ChosenCameraFps = infos[best].Fps;
                    }
                    else
                    {
                        ChosenCameraFps = hasCurrent ? currentInfo.Fps : 0; // keep ARCore's default mode
                    }
                }
            }
            catch (System.Exception e)
            {
                _configDone = true; // do not retry every frame
                UnityEngine.Debug.LogWarning("Camera configuration choice failed: " + e.Message);
            }
            finally
            {
                if (configs.IsCreated) configs.Dispose();
            }
        }

        private void LateUpdate()
        {
            TrySelectCameraConfiguration();
            if (_tracker == null || _arCamera == null) return;
            Transform t = _arCamera.transform;
            bool tracking = ArTracking();
            double now = Time.unscaledTimeAsDouble;
            History.OnFrame(now, tracking, tracking ? string.Empty : NotTrackingReasonLabel(), t.position, t.rotation);
            _tracker.Update(now, t.position, t.rotation, tracking);
            if (_tracker.HasPose) transform.SetPositionAndRotation(_tracker.Position, _tracker.Rotation);
        }
    }
}
