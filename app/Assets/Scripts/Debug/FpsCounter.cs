namespace DiyVr.Diagnostics
{
    /// <summary>
    /// Smooths frame rate over a fixed time window. Pure maths, no Unity calls,
    /// so it can be tested in EditMode. Feed it one frame time per frame.
    /// </summary>
    public sealed class FpsCounter
    {
        public const float DefaultWindowSeconds = 0.5f;

        private readonly float _window;
        private float _elapsed;
        private int _frames;

        public FpsCounter(float windowSeconds = DefaultWindowSeconds)
        {
            _window = windowSeconds > 0f ? windowSeconds : DefaultWindowSeconds;
        }

        /// <summary>Smoothed frames per second. 0 until the first window completes.</summary>
        public float Fps { get; private set; }

        /// <summary>Smoothed average frame time in milliseconds. 0 until the first window completes.</summary>
        public float FrameMs { get; private set; }

        /// <summary>Adds one frame. Returns true when this frame closed a window and the values updated.</summary>
        public bool AddFrame(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                return false;

            _elapsed += deltaSeconds;
            _frames++;

            if (_elapsed < _window)
                return false;

            Fps = _frames / _elapsed;
            FrameMs = _elapsed / _frames * 1000f;
            _elapsed = 0f;
            _frames = 0;
            return true;
        }

        public void Reset()
        {
            _elapsed = 0f;
            _frames = 0;
            Fps = 0f;
            FrameMs = 0f;
        }
    }
}
