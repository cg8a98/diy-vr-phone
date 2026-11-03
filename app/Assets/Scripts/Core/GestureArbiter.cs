namespace DiyVr.Core
{
    /// <summary>A completed touch gesture, after each feature's own detector has recognised it.</summary>
    public enum TouchGesture { None, ThreeFingerTap, DoubleTap, TwoFingerTap }

    /// <summary>The feature that owns a gesture.</summary>
    public enum GestureTarget { None, Overlay, Passthrough, LensTool }

    /// <summary>
    /// The one touch-gesture plan, in plain C# so it can be unit tested:
    ///   three-finger tap -> debug overlay, double tap anywhere -> passthrough, two-finger tap -> lens tool.
    /// While the lens tool is open it gets every gesture and nothing else reacts.
    /// Each feature still detects its own gesture, then asks <see cref="Route"/> whether it may act.
    /// </summary>
    public sealed class GestureArbiter
    {
        /// <summary>The instance shared by the whole app.</summary>
        public static readonly GestureArbiter Shared = new GestureArbiter();

        /// <summary>Set by the lens tool when it opens or closes.</summary>
        public bool LensToolOpen { get; set; }

        public GestureTarget Route(TouchGesture gesture)
        {
            if (gesture == TouchGesture.None) return GestureTarget.None;
            if (LensToolOpen) return GestureTarget.LensTool;
            switch (gesture)
            {
                case TouchGesture.ThreeFingerTap: return GestureTarget.Overlay;
                case TouchGesture.DoubleTap: return GestureTarget.Passthrough;
                case TouchGesture.TwoFingerTap: return GestureTarget.LensTool;
                default: return GestureTarget.None;
            }
        }

        /// <summary>True when <paramref name="target"/> may act on <paramref name="gesture"/> right now.</summary>
        public bool Allows(GestureTarget target, TouchGesture gesture)
        {
            return Route(gesture) == target;
        }
    }
}
