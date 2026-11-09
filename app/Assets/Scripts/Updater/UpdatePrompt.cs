using System;
using DiyVr.Stereo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DiyVr.Updater
{
    /// <summary>
    /// The head-locked update prompt (world-space canvas 1.9 m ahead, in front of the 2.0 m panels).
    /// While it is showing, <see cref="IsOpen"/> is true and GamepadControls hands A/B to it instead of
    /// the lens tool / cube actions. A = install, B = later, one finger tap on the screen = install.
    /// </summary>
    public sealed class UpdatePrompt : MonoBehaviour
    {
        /// <summary>True while the prompt is on screen. Checked by GamepadControls so A/B are not used twice.</summary>
        public static bool IsOpen { get; private set; }

        private const float Distance = 1.9f;
        private const float Width = 1.3f;
        private const float Height = 0.45f;

        public event Action InstallPressed;
        public event Action LaterPressed;

        private GameObject _panel;
        private Text _label;
        private string _text = string.Empty;
        private bool _buttonsActive;

        /// <summary>Shows the prompt. <paramref name="buttonsActive"/> false while downloading (status text only).</summary>
        public void Show(string text, bool buttonsActive)
        {
            _text = text;
            _buttonsActive = buttonsActive;
            IsOpen = true;
            if (_label != null) _label.text = _text;
        }

        public void Hide()
        {
            IsOpen = false;
            if (_panel != null) _panel.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            EnsurePanel();
            if (_panel != null)
            {
                if (!_panel.activeSelf) _panel.SetActive(true);
                _label.text = _text;
            }
            if (!_buttonsActive) return;

            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonSouth.wasPressedThisFrame) { Fire(InstallPressed); return; }
                if (pad.buttonEast.wasPressedThisFrame) { Fire(LaterPressed); return; }
            }
            var screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame && screen.touches.Count > 0)
            {
                // One finger only: the 2- and 3-finger gestures belong to other features.
                int down = 0;
                for (int i = 0; i < screen.touches.Count; i++) if (screen.touches[i].press.isPressed) down++;
                if (down == 1) Fire(InstallPressed);
            }
        }

        private static void Fire(Action a) { if (a != null) a(); }

        private void EnsurePanel()
        {
            if (_panel != null) return;
            var rig = FindFirstObjectByType<StereoRig>();
            if (rig == null) return;

            _panel = new GameObject("UpdatePromptPanel");
            _panel.transform.SetParent(rig.transform, false);
            _panel.transform.localPosition = new Vector3(0f, 0f, Distance);
            var canvas = _panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            var rt = _panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 1000f * Height / Width);
            rt.localScale = Vector3.one * (Width / 1000f);

            var bg = new GameObject("Background");
            bg.transform.SetParent(_panel.transform, false);
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var tx = new GameObject("Text");
            tx.transform.SetParent(_panel.transform, false);
            _label = tx.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 34;
            _label.color = Color.white;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            var txRt = tx.GetComponent<RectTransform>();
            txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
            txRt.offsetMin = new Vector2(30f, 15f); txRt.offsetMax = new Vector2(-30f, -15f);
        }

        private void OnDestroy()
        {
            IsOpen = false;
            if (_panel != null) Destroy(_panel);
        }
    }
}
