using System.Text;
using DiyVr.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

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

        // Current ARCore camera configuration, e.g. "60 fps 1920x1080". The camera rate can differ from the app FPS.
        private string ReadCameraLabel()
        {
            if (_cameraManager == null) _cameraManager = FindFirstObjectByType<ARCameraManager>();
            if (_cameraManager == null) return "n/a";
            var cfg = _cameraManager.currentConfiguration;
            if (!cfg.HasValue) return "n/a";
            object fps = cfg.Value.framerate; // int? in AR Foundation; boxing keeps this compile-safe
            string rate = fps is int ? ((int)fps).ToString() + " fps" : "? fps";
            return rate + " " + cfg.Value.width + "x" + cfg.Value.height;
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
            _sb.Append("Tracking ").Append(ARSession.state);
            if (ARSession.state == ARSessionState.SessionInitializing || ARSession.state == ARSessionState.SessionTracking)
                _sb.Append(" / ").Append(ARSession.notTrackingReason);
            _sb.Append('\n');
            _sb.Append("Camera ").Append(_cameraLabel).Append('\n');
            _sb.Append("Thermal ").Append(_thermalLabel).Append('\n');
            _sb.Append("Build ").Append(Application.version);
            return _sb.ToString();
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
