namespace DiyVr.Diagnostics
{
    /// <summary>
    /// Recognises the overlay toggle gesture from per-frame touch data: a quick three-finger tap.
    /// No Unity input calls, so it can be tested in EditMode.
    /// </summary>
    public sealed class OverlayToggleDetector
    {
        public const float TapMaxSeconds = 0.35f;

        private bool _active;
        private float _startTime;
        private int _maxTouches;

        /// <summary>
        /// Call once per frame with the number of fingers currently pressed.
        /// Returns true on the frame a three-finger tap completes (all fingers lifted within <see cref="TapMaxSeconds"/>).
        /// </summary>
        public bool Update(float time, int touchCount)
        {
            if (!_active)
            {
                if (touchCount > 0)
                {
                    _active = true;
                    _startTime = time;
                    _maxTouches = touchCount;
                }
                return false;
            }

            if (touchCount > _maxTouches)
                _maxTouches = touchCount;

            if (touchCount > 0)
                return false;

            // All fingers lifted: the gesture is over.
            _active = false;
            return _maxTouches == 3 && time - _startTime <= TapMaxSeconds;
        }
    }
}
