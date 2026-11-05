using System.Text;
using DiyVr.Core;
using DiyVr.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Diagnostics
{
    /// <summary>
    /// On-screen debug readout: FPS, frame time, ARCore tracking state, thermal status, build version.
    /// Toggle with a three-finger tap (routed by GestureArbiter) or <see cref="Toggle"/>.
    /// The text is built separately from the drawing (<see cref="BuildText"/>) so a world-space
    /// version can reuse it later.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool startVisible = true;
        [SerializeField] private float thermalPollSeconds = 1f;

        private readonly FpsCounter _fps = new FpsCounter();
        private readonly ThermalStatusReader _thermal = new ThermalStatusReader();
        private readonly OverlayToggleDetector _toggle = new OverlayToggleDetector();
        private readonly StringBuilder _sb = new StringBuilder(160);

        private string _thermalLabel = ThermalStatusReader.UnavailableLabel;
        private float _nextThermalPoll;
        private ARCameraManager _cameraManager;
        private HeadPoseDriver _headPose;
        private string _cameraLabel = "n/a";
        private string _text = string.Empty;
        private GUIStyle _style;

        public bool Visible { get; private set; }

        /// <summary>Latest overlay text, refreshed every frame while visible.</summary>
        public string Text => _text;

        private void Awake()
        {
            Visible = startVisible;
        }

        public void Toggle()
        {
            SetVisible(!Visible);
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
        }

        private void Update()
        {
            PollToggleGesture();

            if (!Visible)
                return;

            _fps.AddFrame(Time.unscaledDeltaTime);

            if (Time.unscaledTime >= _nextThermalPoll)
            {
                _nextThermalPoll = Time.unscaledTime + thermalPollSeconds;
                _thermalLabel = _thermal.ReadLabel();
                _cameraLabel = ReadCameraLabel();
            }

            _text = BuildText();
        }

        // Current ARCore camera configuration, e.g. "30 fps 640x480 FOV 68x53". The camera rate can differ from the app FPS.
        private string ReadCameraLabel()
        {
            if (_cameraManager == null) _cameraManager = FindFirstObjectByType<ARCameraManager>();
            if (_cameraManager == null) return "n/a";
            var cfg = _cameraManager.currentConfiguration;
            if (!cfg.HasValue) return "n/a";
            object fps = cfg.Value.framerate; // int? in AR Foundation; boxing keeps this compile-safe
            string rate = fps is int ? ((int)fps).ToString() + " fps" : "? fps";
            string label = rate + " " + cfg.Value.width + "x" + cfg.Value.height;
            XRCameraIntrinsics k;
            if (_cameraManager.TryGetIntrinsics(out k))
            {
                // Which physical lens ARCore uses shows up as the field of view of the camera image.
                Vector2 fov = CameraFov.Degrees(k.focalLength.x, k.focalLength.y, k.principalPoint.x, k.principalPoint.y,
                    k.resolution.x, k.resolution.y);
                if (fov.x > 0f) label += " FOV " + fov.x.ToString("0") + "x" + fov.y.ToString("0");
            }
            return label;
        }

        private void PollToggleGesture()
        {
            var screen = Touchscreen.current;
            if (screen == null)
                return;

            int count = 0;
            var touches = screen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.isPressed)
                    count++;
            }

            if (_toggle.Update(Time.unscaledTime, count)
                && GestureArbiter.Shared.Allows(GestureTarget.Overlay, TouchGesture.ThreeFingerTap))
                Toggle();
        }

        /// <summary>Builds the multi-line readout from the current values.</summary>
        public string BuildText()
        {
            _sb.Length = 0;
            _sb.Append("FPS ").Append(_fps.Fps.ToString("0.0"))
               .Append("  (").Append(_fps.FrameMs.ToString("0.0")).Append(" ms)\n");
            _sb.Append("Display ").Append(Screen.currentResolution.refreshRateRatio.value.ToString("0")).Append(" Hz (asked ")
               .Append(AppBootstrap.RequestedHz > 0 ? AppBootstrap.RequestedHz.ToString("0") : "none")
               .Append(", mode ").Append(AndroidDisplayMode.RequestedModeId)
               .Append(", req ").Append(AppBootstrap.DisplayRequests)
               .Append(string.IsNullOrEmpty(AndroidDisplayMode.LastError) ? ")" : ") err").Append('\n');
            _sb.Append("Tracking ").Append(ARSession.state);
            if (ARSession.state == ARSessionState.SessionInitializing || ARSession.state == ARSessionState.SessionTracking)
                _sb.Append(" / ").Append(ARSession.notTrackingReason);
            _sb.Append('\n');
            AppendLossHistory(_sb);
            _sb.Append("Camera ").Append(_cameraLabel).Append('\n');
            _sb.Append("Thermal ").Append(_thermalLabel).Append('\n');
            _sb.Append("Build ").Append(Application.version);
            return _sb.ToString();
        }

        // "Lost 3x, last InsufficientFeatures 12s ago for 0.4s, jump 8cm 2deg (max 15cm), drift 0.3 cm/s" (1.20).
        private void AppendLossHistory(StringBuilder sb)
        {
            if (_headPose == null) _headPose = FindFirstObjectByType<HeadPoseDriver>();
            TrackingHistory h = _headPose != null ? _headPose.History : null;
            if (h == null)
            {
                sb.Append("Lost n/a\n");
                return;
            }
            sb.Append("Lost ").Append(h.LossCount).Append('x');
            if (h.LossCount > 0)
            {
                double ago = Time.unscaledTimeAsDouble - h.LastLossStartTime;
                sb.Append(", last ").Append(string.IsNullOrEmpty(h.LastLossReason) ? "?" : h.LastLossReason)
                  .Append(' ').Append(ago.ToString("0")).Append("s ago for ")
                  .Append(h.LastLossSeconds.ToString("0.0")).Append(h.InLoss ? "s+" : "s");
                if (!h.InLoss || h.MaxJumpCm > 0f)
                    sb.Append(", jump ").Append(h.LastJumpCm.ToString("0")).Append("cm ")
                      .Append(h.LastJumpDeg.ToString("0")).Append("deg (max ")
                      .Append(h.MaxJumpCm.ToString("0")).Append("cm)");
            }
            sb.Append(", drift ").Append(h.DriftCmPerSec.ToString("0.0")).Append(" cm/s\n");
        }

        private void OnGUI()
        {
            if (!Visible)
                return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false,
                    richText = false
                };
            }

            _style.fontSize = Mathf.Max(14, Screen.height / 36);
            float pad = _style.fontSize * 0.5f;
            var content = new GUIContent(_text);
            Vector2 size = _style.CalcSize(content);
            GUI.Box(new Rect(pad, pad, size.x + pad, size.y + pad), content, _style);
        }
    }
}
