using System;
using UnityEngine;

namespace DiyVr.Gestures
{
    /// <summary>
    /// Detects a thumb-index pinch from fingertip positions. Pure C#, no MonoBehaviour.
    /// The distance is divided by a hand scale (e.g. wrist to middle knuckle) so the result
    /// does not depend on how far the hand is from the camera or which units are used.
    /// Uses two thresholds (hysteresis) plus a debounce so noisy tracking does not chatter.
    /// </summary>
    public sealed class PinchDetector
    {
        /// <summary>Pinch starts when distance/handScale drops below this.</summary>
        public float StartRatio = 0.25f;
        /// <summary>Pinch ends when distance/handScale rises above this. Must be above StartRatio.</summary>
        public float EndRatio = 0.40f;
        /// <summary>Seconds a condition must hold before the state changes.</summary>
        public float DebounceSeconds = 0.05f;
        /// <summary>Seconds without data before an active pinch ends.</summary>
        public float LostTimeoutSeconds = 0.2f;

        public bool IsPinching { get; private set; }
        /// <summary>0 at EndRatio or wider, 1 at StartRatio or tighter. 0 when no data.</summary>
        public float Strength { get; private set; }
        /// <summary>Last valid ratio, or NaN if none yet.</summary>
        public float LastRatio { get; private set; } = float.NaN;

        public event Action PinchStarted;
        public event Action PinchEnded;

        float _lastDataTime;
        bool _pending;
        float _pendingSince;

        /// <summary>Feed one tracking frame. Invalid input (NaN, infinity, scale not above 0) is ignored.</summary>
        public void Update(Vector3 thumbTip, Vector3 indexTip, float handScale, float time)
        {
            if (!IsFinite(time) || !IsFinite(handScale) || handScale <= 0f
                || !IsFinite(thumbTip) || !IsFinite(indexTip))
                return;

            float ratio = (thumbTip - indexTip).magnitude / handScale;
            if (!IsFinite(ratio))
                return;

            _lastDataTime = time;
            LastRatio = ratio;

            float span = EndRatio - StartRatio;
            Strength = span > 0f ? Mathf.Clamp01((EndRatio - ratio) / span) : (ratio < StartRatio ? 1f : 0f);

            bool wantsChange = IsPinching ? ratio > EndRatio : ratio < StartRatio;
            if (!wantsChange)
            {
                _pending = false;
                return;
            }

            if (!_pending)
            {
                _pending = true;
                _pendingSince = time;
            }

            if (time - _pendingSince >= DebounceSeconds)
                SetPinching(!IsPinching);
        }

        /// <summary>Call when the hand is not tracked this frame. Ends a pinch after LostTimeoutSeconds without data.</summary>
        public void UpdateLost(float time)
        {
            if (!IsFinite(time))
                return;
            _pending = false;
            if (IsPinching && time - _lastDataTime >= LostTimeoutSeconds)
                SetPinching(false);
        }

        /// <summary>Forget everything. Ends an active pinch (raising PinchEnded).</summary>
        public void Reset()
        {
            _pending = false;
            LastRatio = float.NaN;
            Strength = 0f;
            if (IsPinching)
                SetPinching(false);
        }

        void SetPinching(bool value)
        {
            _pending = false;
            if (IsPinching == value)
                return;
            IsPinching = value;
            if (!value)
                Strength = 0f;
            if (value) PinchStarted?.Invoke();
            else PinchEnded?.Invoke();
        }

        static bool IsFinite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        static bool IsFinite(Vector3 v) => IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
    }
}
