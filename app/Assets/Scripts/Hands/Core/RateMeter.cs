namespace DiyVr.Hands
{
    /// <summary>
    /// Events per second from event timestamps (seconds), over the last N events. Drops to 0 when no event
    /// has arrived for <see cref="StaleSeconds"/>.
    /// </summary>
    public sealed class RateMeter
    {
        public const double StaleSeconds = 2.0;

        private readonly double[] _times;
        private int _next;
        private int _count;

        public RateMeter(int window = 16)
        {
            _times = new double[window < 2 ? 2 : window];
        }

        public void Tick(double nowSeconds)
        {
            _times[_next] = nowSeconds;
            _next = (_next + 1) % _times.Length;
            if (_count < _times.Length) _count++;
        }

        /// <summary>Events per second as of <paramref name="nowSeconds"/>.</summary>
        public double Rate(double nowSeconds)
        {
            if (_count < 2) return 0.0;
            int newest = (_next - 1 + _times.Length) % _times.Length;
            if (nowSeconds - _times[newest] > StaleSeconds) return 0.0;
            int oldest = _count == _times.Length ? _next : 0;
            double span = _times[newest] - _times[oldest];
            return span <= 0.0 ? 0.0 : (_count - 1) / span;
        }

        public void Clear()
        {
            _next = 0;
            _count = 0;
        }
    }
}
