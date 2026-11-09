using System.Globalization;

namespace DiyVr.Hands
{
    /// <summary>
    /// Latest hand tracking numbers, written by the hand tracking service and read by the debug overlay.
    /// Lives in the plugin-free assembly so the overlay does not need the MediaPipe package.
    /// Written from the main thread only.
    /// </summary>
    public static class HandsStatus
    {
        public static bool Active;
        public static int HandCount;
        public static double LandmarkFps;
        public static double InferenceMs;
        /// <summary>Why hand tracking is off.</summary>
        public static string Reason = "not started";

        /// <summary>"Hands 1, 24 fps, 18 ms" or "Hands off: model missing".</summary>
        public static string Line()
        {
            return Format(Active, HandCount, LandmarkFps, InferenceMs, Reason);
        }

        public static string Format(bool active, int handCount, double fps, double ms, string reason)
        {
            if (!active)
                return "Hands off: " + (string.IsNullOrEmpty(reason) ? "unknown" : reason);
            return "Hands " + handCount.ToString(CultureInfo.InvariantCulture)
                + ", " + fps.ToString("0", CultureInfo.InvariantCulture) + " fps, "
                + ms.ToString("0", CultureInfo.InvariantCulture) + " ms";
        }

        public static void SetOff(string reason)
        {
            Active = false;
            HandCount = 0;
            LandmarkFps = 0;
            InferenceMs = 0;
            Reason = reason;
        }
    }
}
