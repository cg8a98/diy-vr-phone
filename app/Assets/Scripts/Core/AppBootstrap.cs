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
        }
    }
}
