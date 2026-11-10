using System.Collections.Generic;
using DiyVr.Diagnostics;
using DiyVr.Passthrough;
using DiyVr.Stereo;
using DiyVr.Stereo.LensTool;
using DiyVr.Tracking;
using DiyVr.Updater;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiyVr.App
{
    /// <summary>
    /// Xbox (or any standard) Bluetooth controller for the lens tool and the toggles. Reads Gamepad.current,
    /// hands the buttons to <see cref="GamepadMapper"/> and runs the actions it returns. A controller is not a
    /// touch gesture, so this does not go through GestureArbiter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamepadControls : MonoBehaviour
    {
        public LensToolController lens;
        public PassthroughController passthrough;
        public DebugOverlay overlay;
        public StereoTestContent testContent;

        /// <summary>Calls LensToolController.Adjust this many times for LB/RB.</summary>
        public const int BigStep = 10;

        private PlaneDebugView _planes;
        private bool _maskAB; // A/B stay ignored until released after the update prompt closes
        private readonly GamepadMapper _mapper = new GamepadMapper();
        private readonly List<GamepadAction> _actions = new List<GamepadAction>(8);

        private void Update()
        {
            var pad = Gamepad.current;
            if (pad == null) return;

            var b = new GamepadButtons
            {
                Y = pad.buttonNorth.isPressed,
                A = pad.buttonSouth.isPressed,
                B = pad.buttonEast.isPressed,
                X = pad.buttonWest.isPressed,
                View = pad.selectButton.isPressed,
                DpadUp = pad.dpad.up.isPressed,
                DpadDown = pad.dpad.down.isPressed,
                DpadLeft = pad.dpad.left.isPressed,
                DpadRight = pad.dpad.right.isPressed,
                LeftShoulder = pad.leftShoulder.isPressed,
                RightShoulder = pad.rightShoulder.isPressed,
                RightStick = pad.rightStickButton.isPressed,
            };

            // The update prompt owns A (install) and B (later) while it is showing; hide them from the mapper,
            // and keep hiding them until released so closing the prompt does not also place a cube / close the lens tool.
            // Scan align mode (ticket 3.3) owns the whole controller while it is open: hide every button.
            bool modal = global::DiyVr.Core.ControllerModal.IsOpen;
            if (modal) b = default(GamepadButtons);
            bool prompt = UpdatePrompt.IsOpen || modal;
            if (_maskAB && !prompt && !b.A && !b.B) _maskAB = false;
            if (prompt || _maskAB) { _maskAB = true; b.A = false; b.B = false; }

            _actions.Clear();
            _mapper.Update(b, lens != null && lens.IsOpen, Time.unscaledDeltaTime, _actions);
            for (int i = 0; i < _actions.Count; i++) Run(_actions[i]);
        }

        private void Run(GamepadAction a)
        {
            switch (a)
            {
                case GamepadAction.ToggleLensTool:
                    if (lens != null) { if (lens.IsOpen) lens.Close(); else lens.Open(); }
                    break;
                case GamepadAction.CloseLensTool: if (lens != null) lens.Close(); break;
                case GamepadAction.SelectPrevious: if (lens != null) lens.SelectPrevious(); break;
                case GamepadAction.SelectNext: if (lens != null) lens.SelectNext(); break;
                case GamepadAction.AdjustDown: if (lens != null) lens.Adjust(-1); break;
                case GamepadAction.AdjustUp: if (lens != null) lens.Adjust(1); break;
                case GamepadAction.AdjustDownBig: if (lens != null) for (int i = 0, n = lens.SelectedRow == 0 ? 1 : BigStep; i < n; i++) lens.Adjust(-1); break;
                case GamepadAction.AdjustUpBig: if (lens != null) for (int i = 0, n = lens.SelectedRow == 0 ? 1 : BigStep; i < n; i++) lens.Adjust(1); break;
                case GamepadAction.Save: if (lens != null) lens.Save(); break;
                case GamepadAction.PlaceCube: if (testContent != null) testContent.PlaceCubeInFront(); break;
                case GamepadAction.TogglePassthrough: if (passthrough != null) passthrough.Toggle(); break;
                case GamepadAction.ToggleOverlay: if (overlay != null) overlay.Toggle(); break;
                case GamepadAction.TogglePlanes:
                    if (_planes == null) _planes = FindFirstObjectByType<PlaneDebugView>();
                    if (_planes != null) _planes.Toggle();
                    break;
            }
        }
    }
}
