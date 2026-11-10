using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>Adds the scan loader (START button) to the running app without touching AppRoot.</summary>
    public static class ScanLoadBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<ScanLoadController>() != null) return;
            var go = new GameObject("ScanLoad");
            go.AddComponent<ScanLoadController>();
        }
    }
}
