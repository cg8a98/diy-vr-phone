using System.Collections.Generic;

namespace DiyVr.App
{
    /// <summary>What a controller button press asks the app to do.</summary>
    public enum GamepadAction
    {
        ToggleLensTool,
        CloseLensTool,
        SelectPrevious,
        SelectNext,
        AdjustDown,
        AdjustUp,
        AdjustDownBig,
        AdjustUpBig,
        Save,
        PlaceCube,
        TogglePassthrough,
        ToggleOverlay,
        TogglePlanes,
    }

    /// <summary>Snapshot of the controller buttons for one frame (true = held down).</summary>
    public struct GamepadButtons
    {
        public bool Y, A, B, X, View;
        public bool DpadUp, DpadDown, DpadLeft, DpadRight;
        public bool LeftShoulder, RightShoulder;
        public bool RightStick; // right stick click
    }

    /// <summary>
    /// Pure button-to-action logic, no Unity types. Each press fires once (edge); D-pad left/right repeat while
    /// held: first after <see cref="RepeatDelay"/>, then every <see cref="RepeatInterval"/>.
    /// Lens-tool actions are dropped while the lens tool is closed; Y, X and View always work; A means Save while open and PlaceCube (move the test cube) while closed.
    /// A button already held when the lens tool opens does not fire (only a fresh press does).
    /// </summary>
    public sealed class GamepadMapper
    {
        public const float RepeatDelay = 0.4f;
        public const float RepeatInterval = 0.08f;

        private sealed class Repeater
        {
            public bool Active;     // pressed while the tool was open, still held
            public float Held;      // seconds held
            public int Fired;       // repeats already emitted for this hold
        }

        private GamepadButtons _prev;
        private readonly Repeater _left = new Repeater();
        private readonly Repeater _right = new Repeater();

        /// <summary>Appends the actions for this frame to <paramref name="actions"/>.</summary>
        public void Update(GamepadButtons now, bool lensToolOpen, float deltaTime, List<GamepadAction> actions)
        {
            if (Pressed(now.Y, _prev.Y)) actions.Add(GamepadAction.ToggleLensTool);
            if (Pressed(now.X, _prev.X)) actions.Add(GamepadAction.TogglePassthrough);
            if (Pressed(now.View, _prev.View)) actions.Add(GamepadAction.ToggleOverlay);
            if (Pressed(now.RightStick, _prev.RightStick)) actions.Add(GamepadAction.TogglePlanes);

            // Left and right together cancel each other.
            bool left = now.DpadLeft && !now.DpadRight;
            bool right = now.DpadRight && !now.DpadLeft;
            bool leftBefore = _prev.DpadLeft && !_prev.DpadRight;
            bool rightBefore = _prev.DpadRight && !_prev.DpadLeft;

            if (lensToolOpen)
            {
                if (Pressed(now.B, _prev.B)) actions.Add(GamepadAction.CloseLensTool);
                if (Pressed(now.A, _prev.A)) actions.Add(GamepadAction.Save);
                if (Pressed(now.DpadUp, _prev.DpadUp)) actions.Add(GamepadAction.SelectPrevious);
                if (Pressed(now.DpadDown, _prev.DpadDown)) actions.Add(GamepadAction.SelectNext);
                if (Pressed(now.LeftShoulder, _prev.LeftShoulder)) actions.Add(GamepadAction.AdjustDownBig);
                if (Pressed(now.RightShoulder, _prev.RightShoulder)) actions.Add(GamepadAction.AdjustUpBig);
                Step(_left, left, leftBefore, deltaTime, GamepadAction.AdjustDown, actions);
                Step(_right, right, rightBefore, deltaTime, GamepadAction.AdjustUp, actions);
            }
            else
            {
                if (Pressed(now.A, _prev.A)) actions.Add(GamepadAction.PlaceCube);
                Reset(_left);
                Reset(_right);
            }
            _prev = now;
        }

        private static bool Pressed(bool now, bool before) { return now && !before; }

        private static void Reset(Repeater r) { r.Active = false; r.Held = 0f; r.Fired = 0; }

        private static void Step(Repeater r, bool now, bool before, float dt, GamepadAction action, List<GamepadAction> actions)
        {
            if (!now) { Reset(r); return; }
            if (!before)
            {
                Reset(r);
                r.Active = true;
                actions.Add(action);
                return;
            }
            if (!r.Active) return;
            r.Held += dt;
            // One repeat is due at RepeatDelay, then one more per RepeatInterval.
            int due = r.Held < RepeatDelay ? 0 : 1 + (int)((r.Held - RepeatDelay) / RepeatInterval);
            for (; r.Fired < due; r.Fired++) actions.Add(action);
        }
    }
}
