using UnityEngine;

namespace DiyVr.Core
{
    /// <summary>Re-requests the high refresh rate whenever the app regains focus or resumes.</summary>
    public sealed class DisplayModeKeeper : MonoBehaviour
    {
        private void OnApplicationFocus(bool focus)
        {
            if (focus) AppBootstrap.RequestHighestRefreshRate();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) AppBootstrap.RequestHighestRefreshRate();
        }
    }
}
