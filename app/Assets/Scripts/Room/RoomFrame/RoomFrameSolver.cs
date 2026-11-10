using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>The result of one solve: the room frame and how trustworthy it is.</summary>
    public struct RoomSolution
    {
        public RoomPose Pose;
        /// <summary>Marker B's centre in room coordinates: (0, height difference, floor distance from A).</summary>
        public Vector3 MarkerBInRoom;
        /// <summary>The larger scatter of the two marker averages, metres.</summary>
        public float Spread;
    }

    /// <summary>
    /// Collects marker sightings and turns them into a room frame. Each marker has its own window of samples, so the
    /// two markers do not have to be in view at the same moment: look at A, then at B. The two windows must be
    /// filled within <see cref="MaxGapSeconds"/> of each other so tracking drift between them stays small.
    /// Plain C#, no phone needed.
    /// </summary>
    public sealed class RoomFrameSolver
    {
        public const int MarkerCount = 2;
        public const int DefaultWindowSize = 30;
        public const float MaxGapSeconds = 20f;

        private readonly MarkerAverager[] _markers = new MarkerAverager[MarkerCount];

        public RoomFrameSolver() : this(DefaultWindowSize) { }

        public RoomFrameSolver(int windowSize)
        {
            for (int i = 0; i < MarkerCount; i++) _markers[i] = new MarkerAverager(windowSize);
        }

        public int WindowSize { get { return _markers[0].WindowSize; } }

        /// <summary>Samples collected for a marker (0 = A, 1 = B), up to the window size.</summary>
        public int SampleCount(int marker) { return _markers[marker].Count; }

        public void Clear()
        {
            for (int i = 0; i < MarkerCount; i++) _markers[i].Clear();
        }

        /// <summary>Adds one sighting of a marker (world position). False if it was a repeat of the last one.</summary>
        public bool AddSample(int marker, Vector3 worldPosition, float time)
        {
            if (marker < 0 || marker >= MarkerCount) return false;
            return _markers[marker].Add(worldPosition, time);
        }

        /// <summary>The room frame from the current samples. False until both markers have a full, agreeing, recent window.</summary>
        public bool TrySolve(out RoomSolution solution)
        {
            solution = default(RoomSolution);
            Vector3 a, b;
            float spreadA, spreadB;
            if (!_markers[0].TryGetEstimate(out a, out spreadA)) return false;
            if (!_markers[1].TryGetEstimate(out b, out spreadB)) return false;
            if (Mathf.Abs(_markers[0].LastTime - _markers[1].LastTime) > MaxGapSeconds) return false;
            RoomPose pose;
            if (!RoomFrameMath.TryFromMarkers(a, b, out pose)) return false;
            solution.Pose = pose;
            solution.MarkerBInRoom = pose.WorldToRoom(b);
            solution.Spread = Mathf.Max(spreadA, spreadB);
            return true;
        }
    }
}
