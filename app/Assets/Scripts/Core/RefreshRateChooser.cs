namespace DiyVr.Core
{
    /// <summary>Pure helper: picks the display refresh rate to request.</summary>
    public static class RefreshRateChooser
    {
        /// <summary>
        /// Highest refresh rate (Hz) among modes whose size matches width x height.
        /// Returns 0 when the arrays are null, differ in length, or nothing matches.
        /// </summary>
        public static double ChooseHighest(int[] widths, int[] heights, double[] hz, int width, int height)
        {
            if (widths == null || heights == null || hz == null) return 0;
            if (widths.Length != heights.Length || widths.Length != hz.Length) return 0;

            double best = 0;
            for (int i = 0; i < hz.Length; i++)
            {
                if (widths[i] == width && heights[i] == height && hz[i] > best)
                    best = hz[i];
            }
            return best;
        }
    }
}
