using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// Keeps the newest N position samples of one marker and gives a steady average of them.
    /// Samples far from the middle of the group (a bad detection, a tracking jump) are thrown out before averaging.
    /// Plain C#, no phone needed.
    /// </summary>
    public sealed class MarkerAverager
    {
        /// <summary>Samples closer than this to the median are always kept (metres).</summary>
        public const float MinInlierRadius = 0.01f;
        /// <summary>If the kept samples still scatter more than this (metres), there is no estimate.</summary>
        public const float MaxSpread = 0.02f;

        private readonly Vector3[] _samples;
        private int _next;
        private int _count;
        private float _lastTime = float.NegativeInfinity;

        public MarkerAverager(int windowSize)
        {
            _samples = new Vector3[Mathf.Max(3, windowSize)];
        }

        public int WindowSize { get { return _samples.Length; } }
        public int Count { get { return _count; } }
        public bool IsFull { get { return _count == _samples.Length; } }
        /// <summary>Time of the newest sample (the caller's clock), or a very old time if there is none.</summary>
        public float LastTime { get { return _lastTime; } }

        public void Clear()
        {
            _next = 0;
            _count = 0;
            _lastTime = float.NegativeInfinity;
        }

        /// <summary>Adds a sample. Returns false (and adds nothing) if it is exactly the previous sample again, i.e. no new camera frame.</summary>
        public bool Add(Vector3 position, float time)
        {
            if (_count > 0)
            {
                int last = (_next + _samples.Length - 1) % _samples.Length;
                if (_samples[last] == position) return false;
            }
            _samples[_next] = position;
            _next = (_next + 1) % _samples.Length;
            if (_count < _samples.Length) _count++;
            _lastTime = time;
            return true;
        }

        /// <summary>
        /// Average of the good samples once the window is full. <paramref name="spread"/> is how much the good samples
        /// scatter around the average (root mean square, metres). False while the window is not full or when the
        /// samples scatter more than <see cref="MaxSpread"/>.
        /// </summary>
        public bool TryGetEstimate(out Vector3 mean, out float spread)
        {
            mean = Vector3.zero;
            spread = 0f;
            int n = _count;
            if (n < _samples.Length) return false;

            var xs = new float[n];
            var ys = new float[n];
            var zs = new float[n];
            for (int i = 0; i < n; i++) { xs[i] = _samples[i].x; ys[i] = _samples[i].y; zs[i] = _samples[i].z; }
            var median = new Vector3(Median(xs), Median(ys), Median(zs));

            var dist = new float[n];
            var sorted = new float[n];
            for (int i = 0; i < n; i++) { dist[i] = (_samples[i] - median).magnitude; sorted[i] = dist[i]; }
            // 1.4826 * median distance is about one standard deviation for well-behaved noise; keep three of those.
            float limit = Mathf.Max(MinInlierRadius, 3f * 1.4826f * Median(sorted));

            Vector3 sum = Vector3.zero;
            int kept = 0;
            for (int i = 0; i < n; i++)
            {
                if (dist[i] > limit) continue;
                sum += _samples[i];
                kept++;
            }
            if (kept == 0) return false;
            mean = sum / kept;

            float sq = 0f;
            for (int i = 0; i < n; i++)
            {
                if (dist[i] > limit) continue;
                sq += (_samples[i] - mean).sqrMagnitude;
            }
            spread = Mathf.Sqrt(sq / kept);
            // Samples that do not agree with each other (the marker pose was jumping about) give no estimate.
            return spread <= MaxSpread;
        }

        // Sorts the array it is given.
        private static float Median(float[] values)
        {
            System.Array.Sort(values);
            int mid = values.Length / 2;
            return values.Length % 2 == 1 ? values[mid] : 0.5f * (values[mid - 1] + values[mid]);
        }
    }
}
