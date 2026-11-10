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
        /// <summary>Latency breakdown in ms (averages): camera-to-draw total, camera frame age at pick-up (-1 unknown), image convert. 0 total = not measured yet.</summary>
        public static double LagTotalMs;
        public static double LagAgeMs = -1;
        public static double LagConvertMs;
        /// <summary>Why hand tracking is off.</summary>
        public static string Reason = "not started";
        /// <summary>"GPU" or "CPU": what the landmarker runs on. Empty until known.</summary>
        public static string Delegate = string.Empty;

        /// <summary>"Hands 1, 24 fps, 18 ms" or "Hands off: model missing".</summary>
        public static string Line()
        {
            string line = Format(Active, HandCount, LandmarkFps, InferenceMs, Reason, Delegate);
            if (Active && LagTotalMs > 0)
                line += "\n" + HandLatency.FormatLag(LagTotalMs, LagAgeMs, LagConvertMs, InferenceMs);
            return line;
        }

        public static string Format(bool active, int handCount, double fps, double ms, string reason)
        {
            return Format(active, handCount, fps, ms, reason, string.Empty);
        }

        /// <summary>"Hands 2, 15 fps, 50 ms, GPU" (the delegate is left out when empty).</summary>
        public static string Format(bool active, int handCount, double fps, double ms, string reason, string delegateName)
        {
            if (!active)
                return "Hands off: " + (string.IsNullOrEmpty(reason) ? "unknown" : reason);
            return "Hands " + handCount.ToString(CultureInfo.InvariantCulture)
                + ", " + fps.ToString("0", CultureInfo.InvariantCulture) + " fps, "
                + ms.ToString("0", CultureInfo.InvariantCulture) + " ms"
                + (string.IsNullOrEmpty(delegateName) ? string.Empty : ", " + delegateName);
        }

        public static void SetOff(string reason)
        {
            Active = false;
            HandCount = 0;
            LandmarkFps = 0;
            InferenceMs = 0;
            LagTotalMs = 0;
            LagAgeMs = -1;
            LagConvertMs = 0;
            Reason = reason;
            Delegate = string.Empty;
        }
    }
}
