namespace DiyVr.Core
{
    /// <summary>Pure helper: picks which Android display mode to request.</summary>
    public static class DisplayModeChooser
    {
        /// <summary>
        /// Index of the mode with the highest refresh rate whose physical size equals currentW x currentH.
        /// If none matches the size, the highest rate overall. Ties keep the first mode listed.
        /// Returns -1 when the arrays are null, differ in length, or empty.
        /// </summary>
        public static int ChooseIndex(int[] ids, int[] widths, int[] heights, float[] hz, int currentW, int currentH)
        {
            if (ids == null || widths == null || heights == null || hz == null) return -1;
            int n = ids.Length;
            if (widths.Length != n || heights.Length != n || hz.Length != n || n == 0) return -1;

            int exact = -1;
            int overall = -1;
            for (int i = 0; i < n; i++)
            {
                if (overall < 0 || hz[i] > hz[overall]) overall = i;
                if (widths[i] == currentW && heights[i] == currentH && (exact < 0 || hz[i] > hz[exact])) exact = i;
            }
            return exact >= 0 ? exact : overall;
        }
    }
}
