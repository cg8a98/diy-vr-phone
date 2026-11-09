using System.Collections.Generic;

namespace DiyVr.Tracking
{
    /// <summary>What a detected horizontal plane probably is.</summary>
    public enum PlaneKind { Floor, Desk, Other }

    /// <summary>
    /// Pure plane classification by height relative to the lowest detected plane (assumed to be the floor).
    /// No Unity types. Heights are world Y in metres.
    /// </summary>
    public static class PlaneClassifier
    {
        /// <summary>A plane within this distance of the lowest plane is floor.</summary>
        public const float FloorTolerance = 0.15f;
        /// <summary>Desk surfaces sit between these heights above the floor.</summary>
        public const float DeskMin = 0.45f;
        public const float DeskMax = 1.2f;

        /// <summary>Lowest of the heights, or 0 when there are none.</summary>
        public static float LowestHeight(IList<float> heights)
        {
            if (heights == null || heights.Count == 0) return 0f;
            float lowest = heights[0];
            for (int i = 1; i < heights.Count; i++) if (heights[i] < lowest) lowest = heights[i];
            return lowest;
        }

        /// <summary>
        /// True when a plane should count and be drawn: not merged into a bigger plane, currently tracking, and a real
        /// session plane (not the inactive prefab template, which has no trackable id).
        /// </summary>
        public static bool IsLivePlane(bool isSubsumed, bool isTracking, bool hasTrackableId)
        {
            return !isSubsumed && isTracking && hasTrackableId;
        }

        /// <summary>A plane is drawn only when the owner switched the view on AND it is a live plane.</summary>
        public static bool ShouldShow(bool viewOn, bool isSubsumed, bool isTracking, bool hasTrackableId)
        {
            return viewOn && IsLivePlane(isSubsumed, isTracking, hasTrackableId);
        }

        public static PlaneKind Classify(float planeHeight, float lowestHeight)
        {
            float above = planeHeight - lowestHeight;
            if (above <= FloorTolerance) return PlaneKind.Floor;
            if (above >= DeskMin && above <= DeskMax) return PlaneKind.Desk;
            return PlaneKind.Other;
        }
    }
}
