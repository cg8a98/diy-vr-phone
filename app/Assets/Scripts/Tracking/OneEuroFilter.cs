using UnityEngine;

namespace DiyVr.Tracking
{
    /// <summary>
    /// One Euro filter (Casiez et al. 2012): a low-pass whose cutoff rises with speed, so small noise is
    /// smoothed away at rest and real motion passes with little lag.
    /// Shared maths for the scalar, position and rotation variants.
    /// </summary>
    public static class OneEuro
    {
        /// <summary>Smoothing factor for a low-pass with the given cutoff (Hz) and time step (s). 0..1.</summary>
        public static float Alpha(float cutoffHz, float dt)
        {
            if (dt <= 0f) return 1f;
            float tau = 1f / (2f * Mathf.PI * Mathf.Max(1e-4f, cutoffHz));
            return 1f / (1f + tau / dt);
        }
    }

    /// <summary>One Euro filter for a single float.</summary>
    public sealed class OneEuroFilter
    {
        public float MinCutoff;
        public float Beta;
        public float DerivativeCutoff;

        private bool _init;
        private float _x, _dx;

        public OneEuroFilter(float minCutoff = 1f, float beta = 0f, float derivativeCutoff = 1f)
        {
            MinCutoff = minCutoff; Beta = beta; DerivativeCutoff = derivativeCutoff;
        }

        public float Value { get { return _x; } }
        public void Reset() { _init = false; }

        public float Filter(float x, float dt)
        {
            if (!_init || dt <= 0f)
            {
                _init = true; _x = x; _dx = 0f;
                return x;
            }
            float rawDx = (x - _x) / dt;
            _dx += OneEuro.Alpha(DerivativeCutoff, dt) * (rawDx - _dx);
            float cutoff = MinCutoff + Beta * Mathf.Abs(_dx);
            _x += OneEuro.Alpha(cutoff, dt) * (x - _x);
            return _x;
        }
    }

    /// <summary>One Euro filter for a position. The cutoff follows the speed of the whole vector (no axis skew).</summary>
    public sealed class OneEuroPositionFilter
    {
        public float MinCutoff;
        public float Beta;
        public float DerivativeCutoff;

        private bool _init;
        private Vector3 _x, _dx;

        public OneEuroPositionFilter(float minCutoff = 1f, float beta = 0f, float derivativeCutoff = 1f)
        {
            MinCutoff = minCutoff; Beta = beta; DerivativeCutoff = derivativeCutoff;
        }

        public void Reset() { _init = false; }

        public Vector3 Filter(Vector3 x, float dt)
        {
            if (!_init || dt <= 0f)
            {
                _init = true; _x = x; _dx = Vector3.zero;
                return x;
            }
            Vector3 rawDx = (x - _x) / dt;
            _dx += OneEuro.Alpha(DerivativeCutoff, dt) * (rawDx - _dx);
            float cutoff = MinCutoff + Beta * _dx.magnitude;
            _x += OneEuro.Alpha(cutoff, dt) * (x - _x);
            return _x;
        }
    }

    /// <summary>
    /// One Euro filter for a rotation. Works on the angle between the filtered and new rotation, so there is no
    /// quaternion sign flip or axis skew. Beta is per radian/s of angular speed.
    /// </summary>
    public sealed class OneEuroRotationFilter
    {
        public float MinCutoff;
        public float Beta;
        public float DerivativeCutoff;

        private bool _init;
        private Quaternion _x = Quaternion.identity;
        private float _speed; // rad/s, low-passed

        public OneEuroRotationFilter(float minCutoff = 1f, float beta = 0f, float derivativeCutoff = 1f)
        {
            MinCutoff = minCutoff; Beta = beta; DerivativeCutoff = derivativeCutoff;
        }

        public void Reset() { _init = false; }

        public Quaternion Filter(Quaternion q, float dt)
        {
            if (!_init || dt <= 0f)
            {
                _init = true; _x = q; _speed = 0f;
                return q;
            }
            float angle = AngleRad(_x, q);
            float rawSpeed = angle / dt;
            _speed += OneEuro.Alpha(DerivativeCutoff, dt) * (rawSpeed - _speed);
            float cutoff = MinCutoff + Beta * _speed;
            _x = Quaternion.Slerp(_x, q, OneEuro.Alpha(cutoff, dt));
            return _x;
        }

        /// <summary>Shortest angle in radians between two rotations.</summary>
        public static float AngleRad(Quaternion a, Quaternion b)
        {
            float dot = Mathf.Min(1f, Mathf.Abs(Quaternion.Dot(a, b)));
            return 2f * Mathf.Acos(dot);
        }
    }
}
