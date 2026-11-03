using UnityEngine;

namespace DiyVr.Passthrough
{
    /// <summary>
    /// Pure double-tap logic. Feed it press and release events (seconds, screen pixels, origin bottom-left as
    /// in the Input System). A tap is a short press that barely moves. Taps that start in the overlay corner
    /// (top-left, used by the 1.7 debug overlay toggle) are ignored and break a tap sequence.
    /// </summary>
    public sealed class DoubleTapDetector
    {
        public double maxTapDuration = 0.30;
        public double maxGap = 0.40;
        public float tapSlopPixels = 40f;
        public float maxSeparationPixels = 150f;
        /// <summary>Corner square side as a fraction of the shorter screen side.</summary>
        public float overlayCornerFraction = 0.25f;

        private bool _pressing, _pressIgnored;
        private double _pressTime;
        private Vector2 _pressPos;
        private bool _haveTap;
        private double _lastTapTime;
        private Vector2 _lastTapPos;

        public static bool IsInOverlayCorner(Vector2 pos, int screenWidth, int screenHeight, float fraction)
        {
            float side = Mathf.Min(screenWidth, screenHeight) * fraction;
            return pos.x < side && pos.y > screenHeight - side;
        }

        public void OnPress(double time, Vector2 pos, int screenWidth, int screenHeight)
        {
            _pressing = true;
            _pressTime = time;
            _pressPos = pos;
            _pressIgnored = IsInOverlayCorner(pos, screenWidth, screenHeight, overlayCornerFraction);
            if (_pressIgnored) _haveTap = false;
        }

        /// <summary>Returns true when this release completes a double tap.</summary>
        public bool OnRelease(double time, Vector2 pos)
        {
            if (!_pressing) return false;
            _pressing = false;
            if (_pressIgnored) return false;

            bool isTap = time - _pressTime <= maxTapDuration && (pos - _pressPos).magnitude <= tapSlopPixels;
            if (!isTap) { _haveTap = false; return false; }

            if (_haveTap && time - _lastTapTime <= maxGap && (_pressPos - _lastTapPos).magnitude <= maxSeparationPixels)
            {
                _haveTap = false;
                return true;
            }
            _haveTap = true;
            _lastTapTime = time;
            _lastTapPos = _pressPos;
            return false;
        }

        /// <summary>Forget any press in progress (for example when a second finger lands).</summary>
        public void Cancel()
        {
            _pressing = false;
            _haveTap = false;
        }
    }
}
