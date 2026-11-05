using System.Collections.Generic;
using UnityEngine;

namespace DiyVr.Tracking
{
    /// <summary>
    /// Diagnostic log of ARCore tracking losses for the debug overlay (1.20). Pure logic, no Unity objects.
    /// Feed it every frame with the RAW AR camera pose (before smoothing or holding). It never changes the pose.
    /// </summary>
    public sealed class TrackingHistory
    {
        /// <summary>Length of the window used for the drift measure.</summary>
        public const double DriftWindowSeconds = 2.0;

        private struct Sample
        {
            public double Time;
            public Vector3 Pos;
        }

        private readonly List<Sample> _window = new List<Sample>(256);
        private bool _hasTrackedPose;
        private Vector3 _trackedPos;
        private Quaternion _trackedRot = Quaternion.identity;
        private bool _reasonLatched;
        private double _lossStart;

        /// <summary>Number of tracking losses this session.</summary>
        public int LossCount { get; private set; }
        /// <summary>True while tracking is currently lost.</summary>
        public bool InLoss { get; private set; }
        /// <summary>Reason the last loss STARTED with (first non-None reason seen during it). Empty if no loss yet.</summary>
        public string LastLossReason { get; private set; }
        /// <summary>Time the last loss started (same clock as OnFrame). Valid when LossCount is above 0.</summary>
        public double LastLossStartTime { get; private set; }
        /// <summary>How long the last loss lasted, in seconds. While still lost, the time so far (updated each frame).</summary>
        public double LastLossSeconds { get; private set; }
        /// <summary>Raw position jump at the last regain, in cm. 0 until a loss has ended.</summary>
        public float LastJumpCm { get; private set; }
        /// <summary>Raw rotation jump at the last regain, in degrees.</summary>
        public float LastJumpDeg { get; private set; }
        /// <summary>Largest position jump at any regain this session, in cm.</summary>
        public float MaxJumpCm { get; private set; }
        /// <summary>Rotation jump (degrees) of the regain that produced <see cref="MaxJumpCm"/>.</summary>
        public float MaxJumpDeg { get; private set; }

        public TrackingHistory()
        {
            LastLossReason = string.Empty;
        }

        /// <summary>Raw position path length per second over the last 2 s of tracked frames, in cm/s. 0 if fewer than two samples.</summary>
        public float DriftCmPerSec
        {
            get
            {
                if (_window.Count < 2) return 0f;
                double span = _window[_window.Count - 1].Time - _window[0].Time;
                if (span <= 1e-6) return 0f;
                float path = 0f;
                for (int i = 1; i < _window.Count; i++)
                    path += Vector3.Distance(_window[i - 1].Pos, _window[i].Pos);
                return (float)(path * 100.0 / span);
            }
        }

        /// <param name="time">Seconds, monotonic.</param>
        /// <param name="tracking">True when ARCore is tracking well.</param>
        /// <param name="reason">Why not tracking (notTrackingReason, or the session state when that is None). Only read while not tracking.</param>
        /// <param name="rawPos">Raw AR camera position in metres.</param>
        /// <param name="rawRot">Raw AR camera rotation.</param>
        public void OnFrame(double time, bool tracking, string reason, Vector3 rawPos, Quaternion rawRot)
        {
            if (!tracking)
            {
                if (!InLoss) StartLoss(time); // also covers a session whose very first frame is already lost
                if (!_reasonLatched && !IsNone(reason))
                {
                    LastLossReason = reason;
                    _reasonLatched = true;
                }
                LastLossSeconds = time - _lossStart;
                _window.Clear(); // do not count the jump as drift
            }
            else
            {
                if (InLoss) EndLoss(time, rawPos, rawRot);
                _trackedPos = rawPos;
                _trackedRot = rawRot;
                _hasTrackedPose = true;
                AddDriftSample(time, rawPos);
            }
        }

        private void StartLoss(double time)
        {
            InLoss = true;
            LossCount++;
            _lossStart = time;
            LastLossStartTime = time;
            LastLossSeconds = 0.0;
            LastLossReason = string.Empty;
            _reasonLatched = false;
        }

        private void EndLoss(double time, Vector3 rawPos, Quaternion rawRot)
        {
            InLoss = false;
            LastLossSeconds = time - _lossStart;
            if (!_hasTrackedPose)
            {
                // Never tracked before this loss: no earlier pose to compare with.
                LastJumpCm = 0f;
                LastJumpDeg = 0f;
                return;
            }
            LastJumpCm = Vector3.Distance(_trackedPos, rawPos) * 100f;
            LastJumpDeg = Quaternion.Angle(_trackedRot, rawRot);
            if (LastJumpCm > MaxJumpCm)
            {
                MaxJumpCm = LastJumpCm;
                MaxJumpDeg = LastJumpDeg;
            }
        }

        private void AddDriftSample(double time, Vector3 pos)
        {
            _window.Add(new Sample { Time = time, Pos = pos });
            int drop = 0;
            while (drop < _window.Count - 1 && time - _window[drop].Time > DriftWindowSeconds) drop++;
            if (drop > 0) _window.RemoveRange(0, drop);
        }

        private static bool IsNone(string reason)
        {
            return string.IsNullOrEmpty(reason) || reason == "None";
        }
    }
}
