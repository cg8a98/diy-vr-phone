using UnityEngine;

namespace DiyVr.Experiments.PlaceObjects
{
    /// <summary>Adds the place-objects controller without touching AppRoot.</summary>
    public static class PlaceObjectsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<PlaceObjectsController>() != null) return;
            new GameObject("PlaceObjects").AddComponent<PlaceObjectsController>();
        }
    }
}
