using System.Collections.Generic;
using DiyVr.Gestures;
using DiyVr.Hands;
using DiyVr.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiyVr.Experiments
{
    /// <summary>
    /// Milestone 2.5. Off by default; the Xbox controller's LEFT stick click switches it on/off. When switched on it puts
    /// five buttons in an arc 1.5 m in front of the head (fixed in the world afterwards). Look at the glowing button and
    /// pinch thumb+index: the pinch is scored against whatever the gaze pointer is on at that moment.
    /// </summary>
    public sealed class FiveButtonTest : MonoBehaviour
    {
        private const float FlashSeconds = 0.35f;
        private const float PinchShownSeconds = 0.5f;
        // TextMesh: roughly characterSize * fontSize * 0.1 / 10 metres of line height; tune if the text looks too big or small.
        private const float LabelCharacterSize = 0.006f;
        private const int LabelFontSize = 64;

        private static readonly Color IdleColor = new Color(0.25f, 0.3f, 0.45f);
        private static readonly Color GazedColor = new Color(0.55f, 0.6f, 0.8f);
        private static readonly Color TargetColor = new Color(1f, 0.8f, 0.1f);
        private static readonly Color HitColor = new Color(0.1f, 0.9f, 0.2f);
        private static readonly Color WrongColor = new Color(0.95f, 0.15f, 0.1f);

        private FiveButtonTrial _trial;
        private readonly Gazeable[] _gazeables = new Gazeable[FiveButtonLayout.ButtonCount];
        private readonly Material[] _materials = new Material[FiveButtonLayout.ButtonCount];
        private GameObject _root;
        private TextMesh _label;
        private bool _on;

        private GazePointer _gaze;
        private HandTrackingService _hands;
        private readonly PinchDetector[] _detectors = { new PinchDetector(), new PinchDetector() };
        private int _lastVersion;

        private int _flashButton = -1;
        private Color _flashColor;
        private bool _flashMiss;
        private float _flashUntil;
        private float _pinchShownUntil;

        private void Update()
        {
            var pad = Gamepad.current;
            if (pad != null && pad.leftStickButton.wasPressedThisFrame) SetOn(!_on);
            if (!_on) return;

            if (_gaze == null) _gaze = FindFirstObjectByType<GazePointer>();
            if (_hands == null) _hands = FindFirstObjectByType<HandTrackingService>();
            FeedPinch();
            Refresh();
        }

        private void SetOn(bool on)
        {
            _on = on;
            if (on)
            {
                if (_gaze == null) _gaze = FindFirstObjectByType<GazePointer>();
                // The rig (GazePointer's object) is the head; fall back to the main camera.
                Transform head = _gaze != null ? _gaze.transform : (Camera.main != null ? Camera.main.transform : null);
                if (head == null) { _on = false; return; }
                if (_trial == null) _trial = new FiveButtonTrial(FiveButtonLayout.ButtonCount, System.Environment.TickCount);
                else _trial.Reset();
                Build(head);
                for (int i = 0; i < _detectors.Length; i++) _detectors[i].Reset();
                _lastVersion = _hands != null ? _hands.ResultVersion : 0;
                _flashButton = -1;
                _pinchShownUntil = 0f;
            }
            else if (_root != null)
            {
                Destroy(_root);
                _root = null;
            }
        }

        /// <summary>Builds the five buttons and the label in front of <paramref name="head"/>. Public so an EditMode test can run it.</summary>
        public void Build(Transform head)
        {
            if (_root != null) Destroy(_root);
            _root = new GameObject("FiveButtonTest Buttons");
            Vector3 headPos = head.position;
            Vector3 fwd = FiveButtonLayout.FlatForward(head.forward);

            for (int i = 0; i < FiveButtonLayout.ButtonCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Button " + (i + 1);
                go.layer = 0;
                go.transform.SetParent(_root.transform, false);
                Vector3 pos = FiveButtonLayout.ButtonPosition(headPos, head.forward, i);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.LookRotation(pos - headPos, Vector3.up);
                go.transform.localScale = new Vector3(FiveButtonLayout.Width, FiveButtonLayout.Width, FiveButtonLayout.Thickness);

                var mr = go.GetComponent<MeshRenderer>();
                _materials[i] = new Material(UnlitShader()) { name = "FiveButtonMat" + i, color = IdleColor };
                mr.sharedMaterial = _materials[i];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var g = go.AddComponent<Gazeable>();
                g.highlight = false; // this component draws its own colours
                _gazeables[i] = g;

                // Number just in front of the face (text reads correctly when its forward points away from the viewer).
                var num = MakeText(_root.transform, (i + 1).ToString(), 0.5f);
                num.transform.position = pos - (pos - headPos).normalized * (FiveButtonLayout.Thickness * 0.6f);
                num.transform.rotation = go.transform.rotation;
            }

            // Summary label above the middle button, facing the head.
            var labelGo = new GameObject("FiveButtonTest Label");
            labelGo.transform.SetParent(_root.transform, false);
            Vector3 mid = FiveButtonLayout.ButtonPosition(headPos, head.forward, FiveButtonLayout.ButtonCount / 2);
            labelGo.transform.position = mid + Vector3.up * FiveButtonLayout.LabelHeightAboveEye;
            labelGo.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            _label = labelGo.AddComponent<TextMesh>();
            // TextMesh requires a MeshRenderer and adds one itself; AddComponent<MeshRenderer>() would return null.
            ConfigureText(_label, labelGo.GetComponent<MeshRenderer>(), 1f);
        }

        private static TextMesh MakeText(Transform parent, string text, float size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            ConfigureText(tm, go.GetComponent<MeshRenderer>(), size);
            tm.text = text;
            return tm;
        }

        private static void ConfigureText(TextMesh tm, MeshRenderer mr, float size)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.fontSize = LabelFontSize;
            tm.characterSize = LabelCharacterSize * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            mr.sharedMaterial = font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Shader UnlitShader()
        {
            Shader s = Shader.Find("DiyVr/StereoUnlit");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }

        // Feed each hand's thumb/index tips to its own detector, only when a new tracking result arrived.
        private void FeedPinch()
        {
            if (_hands == null) return;
            float now = Time.unscaledTime;
            int version = _hands.ResultVersion;
            if (version == _lastVersion)
            {
                // No new result this app frame: only time out a held pinch. UpdateLost here would cancel the debounce
                // every frame between results, so a pinch could never start (bug in v0.1.92-0.1.114).
                for (int i = 0; i < _detectors.Length; i++) _detectors[i].Tick(now);
                return;
            }
            _lastVersion = version;

            var frame = _hands.LatestFrame;
            float aspect = frame.Intrinsics.IsValid ? frame.Intrinsics.Width / frame.Intrinsics.Height : 1f;
            IReadOnlyList<HandPose> hands = _hands.LatestHands;
            bool[] seen = new bool[_detectors.Length];

            for (int h = 0; h < hands.Count && h < _detectors.Length; h++)
            {
                int slot = SlotFor(hands[h], h, seen);
                Vector3 thumb, index; float scale;
                if (!PinchLandmarks.TryGet(hands[h].Landmarks, aspect, out thumb, out index, out scale)) continue;
                seen[slot] = true;
                var d = _detectors[slot];
                bool was = d.IsPinching;
                d.Update(thumb, index, scale, now);
                if (!was && d.IsPinching) OnPinch();
            }
            for (int i = 0; i < _detectors.Length; i++)
                if (!seen[i]) _detectors[i].UpdateLost(now);
        }

        private static int SlotFor(HandPose pose, int index, bool[] taken)
        {
            int slot = pose.Side == HandSide.Right ? 1 : pose.Side == HandSide.Left ? 0 : index;
            if (taken[slot]) slot = 1 - slot;
            return slot;
        }

        private void OnPinch()
        {
            _pinchShownUntil = Time.unscaledTime + PinchShownSeconds;
            int gazed = -1;
            if (_gaze != null && _gaze.CurrentTarget != null)
                gazed = System.Array.IndexOf(_gazeables, _gaze.CurrentTarget);
            TrialOutcome o = _trial.RecordPinch(gazed);
            // Flash the pressed button green/red; a miss (no button) turns the label red instead.
            _flashButton = gazed;
            _flashMiss = o == TrialOutcome.Miss;
            _flashColor = o == TrialOutcome.Hit ? HitColor : WrongColor;
            _flashUntil = Time.unscaledTime + FlashSeconds;
            if (gazed >= 0) _gazeables[gazed].Select();
        }

        private void Refresh()
        {
            if (_root == null || _trial == null) return;
            float now = Time.unscaledTime;
            bool flashing = now < _flashUntil;
            for (int i = 0; i < _materials.Length; i++)
            {
                Color c = IdleColor;
                if (i == _trial.Target) c = TargetColor;
                else if (_gazeables[i] != null && _gazeables[i].IsGazedAt) c = GazedColor;
                if (flashing && i == _flashButton) c = _flashColor;
                _materials[i].color = c;
            }
            bool pinchShown = now < _pinchShownUntil;
            _label.text = (pinchShown ? "PINCH" : PinchRatioText()) + "\n" + _trial.Summary();
            _label.color = flashing && _flashMiss ? WrongColor : (pinchShown ? TargetColor : Color.white);
        }

        // Smallest thumb-index ratio of the tracked hands, so the owner can see how close a pinch gets (starts below 0.25).
        private string PinchRatioText()
        {
            float best = float.NaN;
            for (int i = 0; i < _detectors.Length; i++)
            {
                float r = _detectors[i].LastRatio;
                if (!float.IsNaN(r) && (float.IsNaN(best) || r < best)) best = r;
            }
            return float.IsNaN(best) ? "no hand" : "pinch " + best.ToString("0.00");
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _materials.Length; i++) if (_materials[i] != null) Destroy(_materials[i]);
        }
    }
}
