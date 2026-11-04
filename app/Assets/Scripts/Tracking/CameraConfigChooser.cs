using System.Collections.Generic;

namespace DiyVr.Tracking
{
    /// <summary>One ARCore camera configuration, reduced to the numbers the choice needs.</summary>
    public struct CameraConfigInfo
    {
        public int Width;
        public int Height;
        public int Fps; // 0 when unknown

        public CameraConfigInfo(int width, int height, int fps)
        {
            Width = width;
            Height = height;
            Fps = fps;
        }
    }

    /// <summary>Pure choice of the ARCore camera configuration (no Unity types, easy to test).</summary>
    public static class CameraConfigChooser
    {
        public const int PreferredWidth = 1920;
        public const int PreferredHeight = 1080;

        /// <summary>
        /// Index of the best configuration: highest frame rate first, then the resolution whose pixel count is
        /// closest to 1920x1080. Ties keep the earliest entry. Returns -1 for an empty or null list.
        /// </summary>
        public static int ChooseIndex(IList<CameraConfigInfo> configs)
        {
            if (configs == null || configs.Count == 0) return -1;
            long target = (long)PreferredWidth * PreferredHeight;
            int best = 0;
            for (int i = 1; i < configs.Count; i++)
            {
                if (IsBetter(configs[i], configs[best], target)) best = i;
            }
            return best;
        }

        /// <summary>
        /// True only when the best configuration has a strictly higher frame rate than the one in use.
        /// Same or lower fps, or no current configuration, keeps ARCore's default (switching can drop the display to 60 Hz).
        /// </summary>
        public static bool ShouldSwitch(CameraConfigInfo current, bool hasCurrent, CameraConfigInfo best)
        {
            if (!hasCurrent) return false;
            return best.Fps > current.Fps;
        }

        private static bool IsBetter(CameraConfigInfo a, CameraConfigInfo b, long target)
        {
            if (a.Fps != b.Fps) return a.Fps > b.Fps;
            long da = System.Math.Abs((long)a.Width * a.Height - target);
            long db = System.Math.Abs((long)b.Width * b.Height - target);
            return da < db;
        }
    }
}
