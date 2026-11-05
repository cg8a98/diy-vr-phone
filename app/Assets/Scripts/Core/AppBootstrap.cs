using UnityEngine;

namespace DiyVr.Core
{
    /// <summary>Process-wide runtime settings that cannot live in ProjectSettings.</summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        public const int TargetFrameRate = 120;

        /// <summary>Refresh rate (Hz) last passed to Screen.SetResolution; 0 if no request was made.</summary>
        public static double RequestedHz { get; private set; }

        /// <summary>How many times <see cref="RequestHighestRefreshRate"/> ran (shown in the debug overlay).</summary>
        public static int DisplayRequests { get; private set; }

        private void Awake()
        {
            if (GetComponent<DisplayModeKeeper>() == null) gameObject.AddComponent<DisplayModeKeeper>();
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
            DisplayRequests++;
            // Window-level display mode request; this is what holds 120 Hz on Samsung Adaptive.
            AndroidDisplayMode.Request();
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

                // Prefer a mode of the current size; otherwise any mode with the best rate.
                // Either way keep the current width/height and only change the refresh rate.
                int pick = -1;
                for (int i = 0; i < modes.Length; i++)
                {
                    if (modes[i].refreshRateRatio.value < best) continue;
                    if (pick < 0) pick = i;
                    if (modes[i].width == width && modes[i].height == height) { pick = i; break; }
                }
                if (pick < 0) return;

                RequestedHz = modes[pick].refreshRateRatio.value;
                Screen.SetResolution(width, height, Screen.fullScreenMode, modes[pick].refreshRateRatio);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not request refresh rate: " + e.Message);
            }
        }
    }
}
