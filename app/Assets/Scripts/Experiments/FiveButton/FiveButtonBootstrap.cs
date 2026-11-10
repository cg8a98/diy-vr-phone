using UnityEngine;

namespace DiyVr.Experiments
{
    /// <summary>Adds the five-button test (off until the left stick is clicked) without touching AppRoot.</summary>
    public static class FiveButtonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<FiveButtonTest>() != null) return;
            var go = new GameObject("FiveButtonTest");
            go.AddComponent<FiveButtonTest>();
        }
    }
}
