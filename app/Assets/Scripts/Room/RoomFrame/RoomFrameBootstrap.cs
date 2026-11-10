using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>Adds the room frame (ticket 3.2) when the first scene has loaded, without touching AppRoot.</summary>
    public static class RoomFrameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<RoomFrameController>() != null) return;
            var go = new GameObject("Room Frame");
            go.AddComponent<RoomFrameController>();
        }
    }
}
