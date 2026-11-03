using UnityEngine;

namespace DiyVr.Tracking
{
    /// <summary>
    /// Predicts the head pose a short time ahead (to the moment the frame is shown) from the last few samples.
    /// Velocity is the average over a short window (oldest to newest kept sample), which is steadier than a
    /// two-sample difference. Pure maths, no scene objects.
    /// </summary>
    public sealed class PoseExtrapolator
    {
        private readonly double[] _t;
        private readonly Vector3[] _p;
        private readonly Quaternion[] _r;
        private int _count, _head; // _head = index of next write

        /// <summary>Longest horizon that will be extrapolated (s). Guards against overshoot after a hitch.</summary>
        public float MaxHorizon = 0.05f;
        /// <summary>Samples more than this many seconds older than the newest are not used for velocity.</summary>
        public float MaxWindow = 0.1f;

        public PoseExtrapolator(int capacity = 6)
        {
            capacity = Mathf.Max(2, capacity);
            _t = new double[capacity]; _p = new Vector3[capacity]; _r = new Quaternion[capacity];
        }

        public int Count { get { return _count; } }

        public void Clear() { _count = 0; _head = 0; }

        public void AddSample(double time, Vector3 position, Quaternion rotation)
        {
            // Ignore out-of-order or duplicate timestamps.
            if (_count > 0 && time <= _t[IndexFromNewest(0)]) return;
            _t[_head] = time; _p[_head] = position; _r[_head] = rotation;
            _head = (_head + 1) % _t.Length;
            if (_count < _t.Length) _count++;
        }

        private int IndexFromNewest(int k)
        {
            return ((_head - 1 - k) % _t.Length + _t.Length) % _t.Length;
        }

        /// <summary>Estimated linear velocity (m/s) and angular velocity (rad/s, world-space axis-angle vector).</summary>
        public void EstimateVelocity(out Vector3 linear, out Vector3 angular)
        {
            linear = Vector3.zero; angular = Vector3.zero;
            if (_count < 2) return;
            int newest = IndexFromNewest(0);
            int oldest = newest;
            for (int k = 1; k < _count; k++)
            {
                int i = IndexFromNewest(k);
                if (_t[newest] - _t[i] > MaxWindow) break;
                oldest = i;
            }
            if (oldest == newest) return;
            float dt = (float)(_t[newest] - _t[oldest]);
            linear = (_p[newest] - _p[oldest]) / dt;

            Quaternion delta = _r[newest] * Quaternion.Inverse(_r[oldest]);
            if (delta.w < 0f) delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            delta.ToAngleAxis(out float deg, out Vector3 axis);
            if (float.IsNaN(axis.x) || float.IsInfinity(axis.x) || deg < 1e-4f) return;
            angular = axis.normalized * (deg * Mathf.Deg2Rad / dt);
        }

        /// <summary>Pose predicted <paramref name="horizon"/> seconds after the newest sample.</summary>
        public void Predict(float horizon, out Vector3 position, out Quaternion rotation)
        {
            if (_count == 0) { position = Vector3.zero; rotation = Quaternion.identity; return; }
            int n = IndexFromNewest(0);
            horizon = Mathf.Clamp(horizon, 0f, MaxHorizon);
            EstimateVelocity(out Vector3 lin, out Vector3 ang);
            position = _p[n] + lin * horizon;
            float angle = ang.magnitude * horizon;
            rotation = angle > 1e-6f
                ? Quaternion.AngleAxis(angle * Mathf.Rad2Deg, ang / ang.magnitude) * _r[n]
                : _r[n];
        }
    }
}
