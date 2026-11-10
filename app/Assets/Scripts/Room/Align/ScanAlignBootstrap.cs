using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>Adds scan alignment (ticket 3.3) when the first scene has loaded, without touching AppRoot.</summary>
    public static class ScanAlignBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<ScanAlignController>() != null) return;
            var go = new GameObject("Scan Align");
            go.AddComponent<ScanAlignController>();
        }
    }
}
