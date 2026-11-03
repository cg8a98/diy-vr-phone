using UnityEngine;

namespace DiyVr.Diagnostics
{
    /// <summary>
    /// Recognises the overlay toggle gestures from per-frame touch data:
    /// a quick three-finger tap, or a double tap in the top-left corner.
    /// No Unity input calls, so it can be tested in EditMode.
    /// </summary>
    public sealed class OverlayToggleDetector
    {
        public const float TapMaxSeconds = 0.35f;
        public const float DoubleTapMaxGapSeconds = 0.5f;
        /// <summary>Corner size as a fraction of the screen width and height.</summary>
        public const float CornerFraction = 0.2f;

        private bool _active;
        private float _startTime;
        private int _maxTouches;
        private bool _startInCorner;
        private float _lastCornerTapTime = -1f;

        /// <summary>
        /// Call once per frame. <paramref name="touchPosition"/> is a pixel position of any
        /// pressed touch (origin bottom-left, as Unity reports it). Returns true on the frame a toggle gesture completes.
        /// </summary>
        public bool Update(float time, int touchCount, Vector2 touchPosition, Vector2 screenSize)
        {
            if (!_active)
            {
                if (touchCount > 0)
                {
                    _active = true;
                    _startTime = time;
                    _maxTouches = touchCount;
                    _startInCorner = InCorner(touchPosition, screenSize);
                }
                return false;
            }

            if (touchCount > _maxTouches)
                _maxTouches = touchCount;

            if (touchCount > 0)
                return false;

            // All fingers lifted: the gesture is over.
            _active = false;
            if (time - _startTime > TapMaxSeconds)
            {
                _lastCornerTapTime = -1f;
                return false;
            }

            if (_maxTouches >= 3)
            {
                _lastCornerTapTime = -1f;
                return true;
            }

            if (_maxTouches == 1 && _startInCorner)
            {
                if (_lastCornerTapTime >= 0f && time - _lastCornerTapTime <= DoubleTapMaxGapSeconds)
                {
                    _lastCornerTapTime = -1f;
                    return true;
                }
                _lastCornerTapTime = time;
                return false;
            }

            _lastCornerTapTime = -1f;
            return false;
        }

        public static bool InCorner(Vector2 position, Vector2 screenSize)
        {
            return position.x <= screenSize.x * CornerFraction
                && position.y >= screenSize.y * (1f - CornerFraction);
        }
    }
}
