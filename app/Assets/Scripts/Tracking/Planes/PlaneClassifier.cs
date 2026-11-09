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

        public static PlaneKind Classify(float planeHeight, float lowestHeight)
        {
            float above = planeHeight - lowestHeight;
            if (above <= FloorTolerance) return PlaneKind.Floor;
            if (above >= DeskMin && above <= DeskMax) return PlaneKind.Desk;
            return PlaneKind.Other;
        }
    }
}
