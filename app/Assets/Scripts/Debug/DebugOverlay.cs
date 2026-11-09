using System.Text;
using DiyVr.Core;
using DiyVr.Hands;
using DiyVr.Stereo;
using DiyVr.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Diagnostics
{
    /// <summary>
    /// On-screen debug readout: FPS, frame time, ARCore tracking state, thermal status, build version.
    /// Toggle with a three-finger tap (routed by GestureArbiter) or <see cref="Toggle"/>.
    /// Drawn as a head-locked world-space panel (both eyes see it inside the headset).
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
        private StereoRig _rig;
        private GameObject _panel;
        private Text _label;

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
            _sb.Append(HandsStatus.Line()).Append('\n');
            _sb.Append("Planes ").Append(PlaneDebugView.PlaneCount)
               .Append(PlaneDebugView.Visible ? " (shown)" : " (hidden)").Append('\n');
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

        // Head-locked world-space panel, rendered by both eye cameras (default layer), like the lens tool panel.
        private void EnsurePanel()
        {
            if (_panel != null) return;
            if (_rig == null) _rig = FindFirstObjectByType<StereoRig>();
            if (_rig == null) return;

            _panel = new GameObject("DebugOverlayPanel");
            _panel.transform.SetParent(_rig.transform, false);
            _panel.transform.localPosition = OverlayPlacement.LocalPosition(OverlayPlacement.Distance, OverlayPlacement.DegreesDown);
            _panel.transform.localRotation = OverlayPlacement.LocalRotation(OverlayPlacement.DegreesDown);
            var canvas = _panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = _panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 1000f * OverlayPlacement.Height / OverlayPlacement.Width);
            rt.localScale = Vector3.one * (OverlayPlacement.Width / 1000f);

            var bg = new GameObject("Background");
            bg.transform.SetParent(_panel.transform, false);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var tx = new GameObject("Text");
            tx.transform.SetParent(_panel.transform, false);
            _label = tx.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 26;
            _label.color = Color.white;
            _label.alignment = TextAnchor.UpperLeft;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            var txRt = tx.GetComponent<RectTransform>();
            txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
            txRt.offsetMin = new Vector2(20f, 10f); txRt.offsetMax = new Vector2(-20f, -10f);
        }

        private void LateUpdate()
        {
            if (Visible) EnsurePanel();
            if (_panel == null) return;
            if (_panel.activeSelf != Visible) _panel.SetActive(Visible);
            if (Visible) _label.text = _text;
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }
    }
}
