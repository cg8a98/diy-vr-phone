using System;

namespace DiyVr.Passthrough
{
    public enum PassthroughMode
    {
        /// <summary>Camera view hidden.</summary>
        Off,
        /// <summary>User turned it on. Stays on when tracking returns.</summary>
        ManualOn,
        /// <summary>Shown because tracking was lost. Hides again when tracking returns.</summary>
        AutoOnTrackingLost
    }

    /// <summary>
    /// Pure logic for the passthrough view. Starts Off with tracking assumed good (same as HeadPoseTracker).
    /// Rules:
    ///  - Tracking lost while Off -> AutoOnTrackingLost. Tracking back while Auto -> Off.
    ///  - Tracking changes never touch ManualOn.
    ///  - Toggle: Off -> ManualOn; Auto -> ManualOn (pins it); ManualOn -> Off, or Auto if tracking is
    ///    currently lost (the view must not go dark while the pose is frozen).
    /// </summary>
    public sealed class PassthroughStateMachine
    {
        public PassthroughMode Mode { get; private set; }
        public bool TrackingOk { get; private set; }
        public bool IsVisible { get { return Mode != PassthroughMode.Off; } }

        /// <summary>Raised after every change of <see cref="Mode"/>.</summary>
        public event Action<PassthroughMode> ModeChanged;

        public PassthroughStateMachine()
        {
            TrackingOk = true;
            Mode = PassthroughMode.Off;
        }

        public void Toggle()
        {
            switch (Mode)
            {
                case PassthroughMode.Off: Set(PassthroughMode.ManualOn); break;
                case PassthroughMode.AutoOnTrackingLost: Set(PassthroughMode.ManualOn); break;
                default: Set(TrackingOk ? PassthroughMode.Off : PassthroughMode.AutoOnTrackingLost); break;
            }
        }

        /// <summary>Same as Toggle, but to a wanted state. Asking for the current state does nothing.</summary>
        public void SetManual(bool on)
        {
            if (on) { if (Mode != PassthroughMode.ManualOn) Set(PassthroughMode.ManualOn); }
            else if (Mode == PassthroughMode.ManualOn) Toggle();
        }

        public void SetTracking(bool ok)
        {
            TrackingOk = ok;
            if (!ok && Mode == PassthroughMode.Off) Set(PassthroughMode.AutoOnTrackingLost);
            else if (ok && Mode == PassthroughMode.AutoOnTrackingLost) Set(PassthroughMode.Off);
        }

        private void Set(PassthroughMode m)
        {
            if (m == Mode) return;
            Mode = m;
            var h = ModeChanged;
            if (h != null) h(m);
        }
    }
}
