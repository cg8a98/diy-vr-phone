using System;
using UnityEngine;

namespace DiyVr.Tracking
{
    /// <summary>Tuning for the head pose pipeline. Defaults are a starting point for handheld testing.</summary>
    [Serializable]
    public sealed class HeadPoseSettings
    {
        [Tooltip("Prediction horizon in seconds: about one frame plus pipeline latency. Default 20 ms.")]
        public float predictionSeconds = 0.020f;
        [Tooltip("Position cutoff at rest (Hz). Lower = steadier but laggier when slow.")]
        public float positionMinCutoff = 1.5f;
        [Tooltip("Position speed coefficient. Higher = less lag in motion.")]
        public float positionBeta = 8f;
        [Tooltip("Rotation cutoff at rest (Hz).")]
        public float rotationMinCutoff = 2.0f;
        [Tooltip("Rotation speed coefficient, per rad/s.")]
        public float rotationBeta = 4f;
        [Tooltip("Cutoff for the speed estimate inside the filters (Hz).")]
        public float derivativeCutoff = 1.0f;
        [Tooltip("Turn smoothing and prediction off (raw ARCore pose) for comparison.")]
        public bool bypass;
    }

    /// <summary>
    /// Raw ARCore pose in, smoothed and predicted head pose out. Holds the last pose when tracking is lost
    /// and restarts the filters when it comes back (no smoothing across a jump). Plain C#, unit-tested.
    /// </summary>
    public sealed class HeadPoseTracker
    {
        public readonly HeadPoseSettings Settings;
        private readonly OneEuroPositionFilter _posFilter = new OneEuroPositionFilter();
        private readonly OneEuroRotationFilter _rotFilter = new OneEuroRotationFilter();
        private readonly PoseExtrapolator _extrap = new PoseExtrapolator();

        private bool _hasPose;
        private bool _tracking = true;
        private double _lastTime;
        private Vector3 _outPos;
        private Quaternion _outRot = Quaternion.identity;

        /// <summary>Raised when tracking is lost (false) or regained (true).</summary>
        public event Action<bool> TrackingChanged;

        public HeadPoseTracker(HeadPoseSettings settings = null)
        {
            Settings = settings ?? new HeadPoseSettings();
        }

        /// <summary>True while ARCore reports good tracking.</summary>
        public bool IsTracking { get { return _tracking; } }
        public bool HasPose { get { return _hasPose; } }
        public Vector3 Position { get { return _outPos; } }
        public Quaternion Rotation { get { return _outRot; } }

        /// <summary>
        /// Feeds one frame. When <paramref name="tracking"/> is false the raw pose is ignored and the last output is held.
        /// </summary>
        public void Update(double time, Vector3 rawPosition, Quaternion rawRotation, bool tracking)
        {
            if (tracking != _tracking)
            {
                _tracking = tracking;
                if (tracking)
                {
                    // The pose may have jumped while lost: start clean.
                    _posFilter.Reset(); _rotFilter.Reset(); _extrap.Clear();
                    _hasTimeBase = false;
                }
                var handler = TrackingChanged;
                if (handler != null) handler(tracking);
            }
            if (!tracking) return; // hold last pose

            ApplySettings();
            float dt = _hasTimeBase ? (float)(time - _lastTime) : 0f;
            _lastTime = time;
            _hasTimeBase = true;
            _hasPose = true;

            if (Settings.bypass)
            {
                _outPos = rawPosition; _outRot = rawRotation;
                return;
            }

            Vector3 fp = _posFilter.Filter(rawPosition, dt);
            Quaternion fr = _rotFilter.Filter(rawRotation, dt);
            _extrap.AddSample(time, fp, fr);
            _extrap.Predict(Settings.predictionSeconds, out _outPos, out _outRot);
        }

        private bool _hasTimeBase;

        private void ApplySettings()
        {
            _posFilter.MinCutoff = Settings.positionMinCutoff;
            _posFilter.Beta = Settings.positionBeta;
            _posFilter.DerivativeCutoff = Settings.derivativeCutoff;
            _rotFilter.MinCutoff = Settings.rotationMinCutoff;
            _rotFilter.Beta = Settings.rotationBeta;
            _rotFilter.DerivativeCutoff = Settings.derivativeCutoff;
        }
    }
}
