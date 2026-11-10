using System.Globalization;

namespace DiyVr.Hands
{
    /// <summary>
    /// Timing maths for hand tracking lag (ticket 2.2c). All times are seconds on one clock (see HandClock).
    /// "Lag" is how much older the drawn skeleton is than the passthrough image next to it.
    /// </summary>
    public static class HandLatency
    {
        /// <summary>A camera stamp further than this behind "now" is not trusted (wrong clock).</summary>
        public const double MaxPlausibleAgeSeconds = 0.5;

        /// <summary>Share of the measured age that the skeleton is pushed ahead along its velocity.</summary>
        public const float LeadGain = 0.5f;

        /// <summary>The look-ahead never exceeds this, so a fast jerk cannot throw the skeleton far.</summary>
        public const float MaxLeadSeconds = 0.05f;

        /// <summary>Age of a camera frame in seconds, or -1 when the stamp is missing or on a different clock.</summary>
        public static double AgeSeconds(double imageStamp, double now)
        {
            if (imageStamp <= 0.0) return -1.0;
            double age = now - imageStamp;
            if (age < -0.005 || age > MaxPlausibleAgeSeconds) return -1.0;
            return age < 0.0 ? 0.0 : age;
        }

        /// <summary>
        /// True when the camera frame is newer than the last one processed. Frames without a stamp always count.
        /// Stops the same camera image being run twice while the next one has not arrived (that wastes an
        /// inference slot and delays the real next frame).
        /// </summary>
        public static bool IsNewFrame(double lastStamp, double stamp)
        {
            if (stamp <= 0.0 || lastStamp < 0.0) return true;
            return stamp > lastStamp + 1e-6;
        }

        /// <summary>How far ahead (seconds) to draw, given how old the result is now. 0 when the age is unknown.</summary>
        public static float LeadSeconds(double ageNow)
        {
            if (ageNow <= 0.0) return 0f;
            float lead = (float)ageNow * LeadGain;
            return lead > MaxLeadSeconds ? MaxLeadSeconds : lead;
        }

        /// <summary>"Lag 85 ms = cam 40 + cv 4 + inf 30 + post 11" (cam shown as ? when unknown).</summary>
        public static string FormatLag(double totalMs, double ageMs, double convertMs, double inferenceMs)
        {
            var c = CultureInfo.InvariantCulture;
            double known = (ageMs > 0 ? ageMs : 0) + convertMs + inferenceMs;
            double post = totalMs - known;
            if (post < 0) post = 0;
            return "Lag " + totalMs.ToString("0", c) + " ms = cam " + (ageMs >= 0 ? ageMs.ToString("0", c) : "?")
                + " + cv " + convertMs.ToString("0", c) + " + inf " + inferenceMs.ToString("0", c)
                + " + post " + post.ToString("0", c);
        }
    }
}
