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
        private static AndroidJavaClass _javaSystem;
        private static bool _monoFailed;
#endif

        /// <summary>
        /// "Now" on Android's other clock (System.nanoTime, which stops while the phone sleeps). Only used to tell
        /// which clock the camera stamps are on (ticket 2.2e diagnostics). Elsewhere it is Unity's real time.
        /// </summary>
        public static double NowMonotonic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_monoFailed)
            {
                try
                {
                    if (_javaSystem == null) _javaSystem = new AndroidJavaClass("java.lang.System");
                    return _javaSystem.CallStatic<long>("nanoTime") * 1e-9;
                }
                catch (System.Exception) { _monoFailed = true; }
            }
#endif
            return Time.realtimeSinceStartupAsDouble;
        }

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
