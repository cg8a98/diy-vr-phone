using UnityEngine;

namespace DiyVr.Hands
{
    public enum HandSide
    {
        Unknown = 0,
        Left = 1,
        Right = 2,
    }

    /// <summary>One detected hand: 21 landmarks, x and y normalised 0..1 across the image (y down), z relative depth.</summary>
    public sealed class HandPose
    {
        public const int LandmarkCount = 21;

        public readonly Vector3[] Landmarks;
        public readonly HandSide Side;
        public readonly float Score;

        public HandPose(Vector3[] landmarks, HandSide side, float score)
        {
            Landmarks = landmarks;
            Side = side;
            Score = score;
        }

        /// <summary>Maps MediaPipe's category name ("Left" or "Right") to <see cref="HandSide"/>.</summary>
        public static HandSide ParseSide(string categoryName)
        {
            if (categoryName == "Left") return HandSide.Left;
            if (categoryName == "Right") return HandSide.Right;
            return HandSide.Unknown;
        }
    }
}
