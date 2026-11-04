using UnityEngine;

namespace DiyVr.Core
{
    /// <summary>Process-wide runtime settings that cannot live in ProjectSettings.</summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        public const int TargetFrameRate = 120;

        private void Awake()
        {
            Apply();
        }

        /// <summary>Keeps the screen on and requests the highest refresh rate.</summary>
        public static void Apply()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            RequestHighestRefreshRate();
        }

        /// <summary>
        /// Android ignores targetFrameRate for the display mode, so ask for the highest refresh rate
        /// available at the current screen size.
        /// </summary>
        public static void RequestHighestRefreshRate()
        {
            try
            {
                var modes = Screen.resolutions;
                var w = new int[modes.Length];
                var h = new int[modes.Length];
                var hz = new double[modes.Length];
                for (int i = 0; i < modes.Length; i++)
                {
                    w[i] = modes[i].width;
                    h[i] = modes[i].height;
                    hz[i] = modes[i].refreshRateRatio.value;
                }

                int width = Screen.width;
                int height = Screen.height;
                double best = RefreshRateChooser.ChooseHighest(w, h, hz, width, height);
                if (best <= 0) return;

                for (int i = 0; i < modes.Length; i++)
                {
                    if (modes[i].width == width && modes[i].height == height && modes[i].refreshRateRatio.value >= best)
                    {
                        Screen.SetResolution(width, height, Screen.fullScreenMode, modes[i].refreshRateRatio);
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not request refresh rate: " + e.Message);
            }
        }
    }
}
