using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// Where the room frame sits in the app's (ARCore) world: an origin and a turn around the vertical axis.
    /// Room axes: +Y is up (gravity), +Z points from marker A towards marker B along the floor, +X is to the right of that.
    /// </summary>
    public struct RoomPose
    {
        /// <summary>World position of the room origin (the centre of marker A).</summary>
        public Vector3 Origin;
        /// <summary>Turn around the vertical axis, degrees.</summary>
        public float YawDegrees;

        public RoomPose(Vector3 origin, float yawDegrees)
        {
            Origin = origin;
            YawDegrees = yawDegrees;
        }

        public Quaternion Rotation { get { return Quaternion.Euler(0f, YawDegrees, 0f); } }

        public Vector3 RoomToWorld(Vector3 roomPoint) { return Origin + Rotation * roomPoint; }

        public Vector3 WorldToRoom(Vector3 worldPoint) { return Quaternion.Inverse(Rotation) * (worldPoint - Origin); }

        public Quaternion RoomToWorldRotation(Quaternion roomRotation) { return Rotation * roomRotation; }

        public Quaternion WorldToRoomRotation(Quaternion worldRotation) { return Quaternion.Inverse(Rotation) * worldRotation; }
    }

    /// <summary>Pure maths for the room frame, so it can be unit tested without a phone.</summary>
    public static class RoomFrameMath
    {
        /// <summary>The markers must be at least this far apart along the floor, or the direction between them means nothing.</summary>
        public const float MinHorizontalDistance = 0.3f;

        /// <summary>
        /// Room frame from the two marker centres (world positions). Origin = marker A. Yaw = direction A to B with the
        /// height difference removed, so a marker hung a little higher or lower does not tilt the room.
        /// False if the markers are (almost) above each other.
        /// </summary>
        public static bool TryFromMarkers(Vector3 markerA, Vector3 markerB, out RoomPose pose)
        {
            Vector3 flat = markerB - markerA;
            flat.y = 0f;
            if (flat.magnitude < MinHorizontalDistance)
            {
                pose = new RoomPose(markerA, 0f);
                return false;
            }
            float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            pose = new RoomPose(markerA, yaw);
            return true;
        }

        /// <summary>Smallest difference between two angles, degrees, 0..180.</summary>
        public static float AngleDifference(float aDegrees, float bDegrees)
        {
            return Mathf.Abs(Mathf.DeltaAngle(aDegrees, bDegrees));
        }
    }
}
