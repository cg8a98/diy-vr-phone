namespace DiyVr.Core
{
    /// <summary>Pure helper: picks the display refresh rate to request.</summary>
    public static class RefreshRateChooser
    {
        /// <summary>
        /// Highest refresh rate (Hz) among modes whose size matches width x height. If no mode matches
        /// the size (Screen.width/height can be a scaled size while Screen.resolutions lists the native
        /// panel), falls back to the highest rate across all modes.
        /// Returns 0 when the arrays are null, differ in length, or empty.
        /// </summary>
        public static double ChooseHighest(int[] widths, int[] heights, double[] hz, int width, int height)
        {
            if (widths == null || heights == null || hz == null) return 0;
            if (widths.Length != heights.Length || widths.Length != hz.Length) return 0;

            double exact = 0;
            double overall = 0;
            for (int i = 0; i < hz.Length; i++)
            {
                if (hz[i] > overall)
                    overall = hz[i];
                if (widths[i] == width && heights[i] == height && hz[i] > exact)
                    exact = hz[i];
            }
            return exact > 0 ? exact : overall;
        }
    }
}
