using DiyVr.Stereo;
using UnityEngine;

namespace DiyVr.Interaction
{
    /// <summary>
    /// Adds the gaze pointer without touching AppRoot. Runs AfterSceneLoad (AppRoot does too, so the StereoRig may not
    /// exist yet) and starts a tiny helper that retries for a few seconds, then removes itself.
    /// </summary>
    public static class GazeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<GazeBootstrapRunner>() != null) return;
            var go = new GameObject("GazeBootstrap");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<GazeBootstrapRunner>();
        }
    }
}
