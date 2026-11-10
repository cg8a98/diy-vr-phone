using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// The published room frame. This object's transform IS the room: put anything that belongs to the real room
    /// (the scan, colliders, test objects) under it, or convert points with <see cref="RoomToWorld"/> and
    /// <see cref="WorldToRoom"/>. Until <see cref="IsLocked"/> is true the transform means nothing.
    /// Room axes: origin at the centre of marker A, +Y up, +Z from marker A towards marker B along the floor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomFrame : MonoBehaviour
    {
        /// <summary>The app's room frame, or null if there is none.</summary>
        public static RoomFrame Current { get; private set; }

        /// <summary>True once both markers were seen this session and the frame was set from them.</summary>
        public bool IsLocked { get; private set; }
        public RoomPose Pose { get; private set; }
        /// <summary>
        /// Quality check, metres: how far marker B is from where it was at calibration (seen from marker A).
        /// Small (under about 1 cm) is good; several cm means a marker moved or was measured badly.
        /// </summary>
        public float ResidualMeters { get; private set; }
        /// <summary>How much the marker sightings scattered while averaging, metres. Smaller is steadier.</summary>
        public float SpreadMeters { get; private set; }
        /// <summary>True if the floor height is known (found by ARCore at calibration).</summary>
        public bool HasFloor { get; private set; }
        /// <summary>Floor height in room coordinates (negative: below marker A). Only valid if <see cref="HasFloor"/>.</summary>
        public float FloorY { get; private set; }
        /// <summary>Goes up by one every time the frame is set or refined.</summary>
        public int LockCount { get; private set; }

        /// <summary>Raised whenever the frame is set, refined or cleared.</summary>
        public event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
        }

        /// <summary>Makes this the app's room frame.</summary>
        public void Register()
        {
            Current = this;
        }

        public Vector3 RoomToWorld(Vector3 roomPoint) { return Pose.RoomToWorld(roomPoint); }

        public Vector3 WorldToRoom(Vector3 worldPoint) { return Pose.WorldToRoom(worldPoint); }

        /// <summary>Sets (or refines) the frame. Called by <see cref="RoomFrameController"/>.</summary>
        public void SetLock(RoomPose pose, float residualMeters, float spreadMeters, bool hasFloor, float floorY)
        {
            Pose = pose;
            transform.SetPositionAndRotation(pose.Origin, pose.Rotation);
            ResidualMeters = residualMeters;
            SpreadMeters = spreadMeters;
            HasFloor = hasFloor;
            FloorY = floorY;
            IsLocked = true;
            LockCount++;
            var handler = Changed;
            if (handler != null) handler();
        }

        public void ClearLock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            var handler = Changed;
            if (handler != null) handler();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
