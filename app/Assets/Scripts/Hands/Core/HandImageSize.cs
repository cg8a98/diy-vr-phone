namespace DiyVr.Hands
{
    /// <summary>Picks the size of the RGBA image handed to the hand landmarker.</summary>
    public static class HandImageSize
    {
        public const int DefaultMaxWidth = 640;

        /// <summary>
        /// Keeps the camera size when its width is at most <paramref name="maxWidth"/>, otherwise scales down to
        /// that width keeping the aspect ratio. Both sides are even and at least 2. Invalid input gives 0x0.
        /// </summary>
        public static void Choose(int cameraWidth, int cameraHeight, int maxWidth, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (cameraWidth <= 0 || cameraHeight <= 0 || maxWidth <= 0) return;

            double scale = cameraWidth > maxWidth ? (double)maxWidth / cameraWidth : 1.0;
            width = Even((int)System.Math.Round(cameraWidth * scale));
            height = Even((int)System.Math.Round(cameraHeight * scale));
        }

        private static int Even(int v)
        {
            v &= ~1;
            return v < 2 ? 2 : v;
        }
    }
}
