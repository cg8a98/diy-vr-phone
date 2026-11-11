using System.Collections.Generic;
using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// Removes a second detection of the same real hand. With the low detection confidence needed in dim light the
    /// landmarker sometimes reports one hand twice, which showed as two skeletons on one hand.
    ///
    /// Two detections count as the same hand when the mean distance between their 21 matching landmarks is below
    /// <see cref="DuplicateDistanceRatio"/> hand lengths (wrist to middle knuckle). This was chosen over bounding-box
    /// overlap because it does not depend on how far the hand is from the camera and it stays high for two different
    /// hands that touch or partly cover each other (their boxes overlap, but their fingers are not in the same place).
    /// Pure maths: no Unity objects, safe on the worker thread.
    /// </summary>
    public static class HandDedupe
    {
        /// <summary>Mean landmark distance, in hand lengths, below which two detections are one hand.</summary>
        public const float DuplicateDistanceRatio = 0.35f;

        /// <summary>Smallest hand length used (image-height units), so a degenerate hand cannot divide by zero.</summary>
        public const float MinHandScale = 0.02f;

        public const int Wrist = 0;
        public const int MiddleKnuckle = 9;

        private static readonly int[] PalmPoints = { 0, 5, 9, 13, 17 };

        /// <summary>Wrist to middle knuckle in image-height units (x is multiplied by aspect = width / height).</summary>
        public static float HandScale(Vector3[] landmarks, float aspect)
        {
            if (!IsUsable(landmarks)) return MinHandScale;
            float dx = (landmarks[MiddleKnuckle].x - landmarks[Wrist].x) * aspect;
            float dy = landmarks[MiddleKnuckle].y - landmarks[Wrist].y;
            return Mathf.Max(MinHandScale, Mathf.Sqrt(dx * dx + dy * dy));
        }

        /// <summary>Middle of the palm (wrist and the four finger knuckles) in image-height units.</summary>
        public static Vector2 PalmCentre(Vector3[] landmarks, float aspect)
        {
            if (!IsUsable(landmarks)) return Vector2.zero;
            float x = 0f, y = 0f;
            for (int i = 0; i < PalmPoints.Length; i++)
            {
                x += landmarks[PalmPoints[i]].x * aspect;
                y += landmarks[PalmPoints[i]].y;
            }
            return new Vector2(x / PalmPoints.Length, y / PalmPoints.Length);
        }

        /// <summary>Mean 2D distance between matching landmarks in image-height units.</summary>
        public static float MeanDistance(Vector3[] a, Vector3[] b, float aspect)
        {
            if (!IsUsable(a) || !IsUsable(b)) return float.PositiveInfinity;
            float sum = 0f;
            for (int i = 0; i < HandPose.LandmarkCount; i++)
            {
                float dx = (a[i].x - b[i].x) * aspect;
                float dy = a[i].y - b[i].y;
                sum += Mathf.Sqrt(dx * dx + dy * dy);
            }
            return sum / HandPose.LandmarkCount;
        }

        /// <summary>True when the two detections are the same real hand. Uses the larger of the two hand lengths.</summary>
        public static bool IsDuplicate(HandPose a, HandPose b, float aspect)
        {
            if (a == null || b == null) return false;
            float scale = Mathf.Max(HandScale(a.Landmarks, aspect), HandScale(b.Landmarks, aspect));
            return MeanDistance(a.Landmarks, b.Landmarks, aspect) < DuplicateDistanceRatio * scale;
        }

        /// <summary>
        /// Returns the hands with duplicates removed, in their original order. Of two duplicates the one with the higher
        /// score stays; on equal scores the earlier one stays. Returns the same list when nothing was removed.
        /// </summary>
        public static IReadOnlyList<HandPose> Suppress(IReadOnlyList<HandPose> hands, float aspect)
        {
            if (hands == null || hands.Count < 2) return hands;
            if (!(aspect > 0f)) aspect = 1f;

            bool[] removed = new bool[hands.Count];
            int removedCount = 0;
            for (int i = 0; i < hands.Count; i++)
            {
                if (removed[i]) continue;
                for (int j = i + 1; j < hands.Count; j++)
                {
                    if (removed[j] || !IsDuplicate(hands[i], hands[j], aspect)) continue;
                    removedCount++;
                    if (hands[j].Score > hands[i].Score) { removed[i] = true; break; }
                    removed[j] = true;
                }
            }
            if (removedCount == 0) return hands;

            var kept = new List<HandPose>(hands.Count - removedCount);
            for (int i = 0; i < hands.Count; i++)
                if (!removed[i]) kept.Add(hands[i]);
            return kept;
        }

        private static bool IsUsable(Vector3[] landmarks)
        {
            return landmarks != null && landmarks.Length >= HandPose.LandmarkCount;
        }
    }
}
