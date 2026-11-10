using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// "Now" on the same clock as ARCore camera frame stamps (Android SystemClock.elapsedRealtimeNanos), so a
    /// camera frame's age can be measured. Elsewhere it is Unity's real time. Main thread only.
    /// </summary>
    public static class HandClock
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaClass _systemClock;
        private static bool _failed;
#endif

        public static double Now()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_failed)
            {
                try
                {
                    if (_systemClock == null) _systemClock = new AndroidJavaClass("android.os.SystemClock");
                    return _systemClock.CallStatic<long>("elapsedRealtimeNanos") * 1e-9;
                }
                catch (System.Exception) { _failed = true; }
            }
#endif
            return Time.realtimeSinceStartupAsDouble;
        }
    }
}
