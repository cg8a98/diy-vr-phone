namespace DiyVr.Hands
{
    /// <summary>Average of the last N samples (ring buffer, no allocation after construction).</summary>
    public sealed class RollingAverage
    {
        private readonly double[] _values;
        private int _next;
        private int _count;
        private double _sum;

        public RollingAverage(int window)
        {
            _values = new double[window < 1 ? 1 : window];
        }

        public int Count => _count;

        /// <summary>Average of the stored samples, 0 when empty.</summary>
        public double Average => _count == 0 ? 0.0 : _sum / _count;

        public void Add(double value)
        {
            if (_count == _values.Length) _sum -= _values[_next];
            else _count++;
            _values[_next] = value;
            _sum += value;
            _next = (_next + 1) % _values.Length;
        }

        public void Clear()
        {
            _next = 0;
            _count = 0;
            _sum = 0.0;
        }
    }
}
