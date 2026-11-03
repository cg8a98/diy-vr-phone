using System;

namespace DiyVr.Stereo.LensTool
{
    public enum GestureKind { None, TapLeft, TapRight, StepUp, StepDown, LongPress, TwoFingerTap }

    /// <summary>One sample of the touch screen. Y grows upwards (Unity screen space). Count is the number of fingers down.</summary>
    public struct TouchFrame
    {
        public float Time;
        public int Count;
        public float X, Y;
    }

    /// <summary>
    /// Turns raw touch samples into simple gestures usable with gloves on and a phone in a headset:
    /// tap left or right half, vertical drag (one step per drag distance), long press, two-finger tap.
    /// Pure logic, no Unity types, so it is unit tested.
    /// </summary>
    public sealed class GestureRecognizer
    {
        public const float TapMaxSeconds = 0.35f;
        public const float LongPressSeconds = 0.8f;
        public const float TwoFingerTapMaxSeconds = 0.5f;

        private float _w, _h;
        private bool _down, _moved, _longFired;
        private float _startT, _startX, _startY, _anchorY;
        private int _maxCount;

        public GestureRecognizer(float screenWidth, float screenHeight) { Resize(screenWidth, screenHeight); }

        public void Resize(float screenWidth, float screenHeight) { _w = screenWidth; _h = screenHeight; }

        /// <summary>Vertical drag distance for one step, in pixels.</summary>
        public float StepPixels { get { return 0.12f * _h; } }
        private float SlopPixels { get { return 0.03f * _h; } }

        /// <summary>Feed one sample per frame; returns at most one gesture per call.</summary>
        public GestureKind Update(TouchFrame f)
        {
            if (f.Count <= 0)
            {
                if (!_down) return GestureKind.None;
                _down = false;
                float dur = f.Time - _startT;
                var result = GestureKind.None;
                if (!_moved && !_longFired)
                {
                    if (_maxCount >= 2) { if (dur <= TwoFingerTapMaxSeconds) result = GestureKind.TwoFingerTap; }
                    else if (dur <= TapMaxSeconds) result = _startX < _w * 0.5f ? GestureKind.TapLeft : GestureKind.TapRight;
                }
                return result;
            }

            if (!_down)
            {
                _down = true; _moved = false; _longFired = false;
                _startT = f.Time; _startX = f.X; _startY = f.Y; _anchorY = f.Y; _maxCount = f.Count;
                return GestureKind.None;
            }

            if (f.Count > _maxCount) _maxCount = f.Count;
            if (_maxCount >= 2) return GestureKind.None;

            if (!_moved && (Math.Abs(f.X - _startX) > SlopPixels || Math.Abs(f.Y - _startY) > SlopPixels)) _moved = true;

            float dy = f.Y - _anchorY;
            if (Math.Abs(dy) >= StepPixels)
            {
                int dir = dy > 0 ? 1 : -1;
                _anchorY += dir * StepPixels;
                return dir > 0 ? GestureKind.StepUp : GestureKind.StepDown;
            }

            if (!_moved && !_longFired && f.Time - _startT >= LongPressSeconds)
            {
                _longFired = true;
                return GestureKind.LongPress;
            }
            return GestureKind.None;
        }
    }
}
