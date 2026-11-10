using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// Opens Android's own file picker (the app has no permission to read Downloads by itself) and copies the
    /// chosen file to a path inside the app's private storage. The Java side lives in
    /// Assets/Plugins/Android/ScanPicker.androidlib. Nothing is uploaded anywhere.
    /// </summary>
    public static class ScanPicker
    {
        // Must match the constants in ScanPickerActivity.java.
        public const int Idle = 0;
        public const int Picking = 1;
        public const int Done = 2;
        public const int Cancelled = 3;
        public const int Failed = 4;

#if UNITY_ANDROID && !UNITY_EDITOR
        private const string JavaClass = "com.diyvr.scanpicker.ScanPickerActivity";
#endif

        /// <summary>True on the phone; false in the Unity editor and in tests.</summary>
        public static bool Supported
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Shows the picker. The chosen file is copied to <paramref name="destPath"/>.</summary>
        public static bool Start(string destPath, out string error)
        {
            error = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var cls = new AndroidJavaClass(JavaClass))
                {
                    cls.CallStatic("start", activity, destPath);
                }
                return true;
            }
            catch (System.Exception e)
            {
                error = e.Message;
                return false;
            }
#else
            error = "picking a file only works on the phone";
            return false;
#endif
        }

        /// <summary>One of <see cref="Idle"/>, <see cref="Picking"/>, <see cref="Done"/>, <see cref="Cancelled"/>, <see cref="Failed"/>.</summary>
        public static int State()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var cls = new AndroidJavaClass(JavaClass)) return cls.CallStatic<int>("getState");
            }
            catch (System.Exception)
            {
                return Failed;
            }
#else
            return Idle;
#endif
        }

        /// <summary>The picked file's name as Android shows it (for example "bedroom.glb").</summary>
        public static string PickedName()
        {
            return GetString("getDisplayName");
        }

        public static string Error()
        {
            return GetString("getError");
        }

        /// <summary>Back to <see cref="Idle"/> after a result was read.</summary>
        public static void Reset()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var cls = new AndroidJavaClass(JavaClass)) cls.CallStatic("reset");
            }
            catch (System.Exception) { }
#endif
        }

        private static string GetString(string method)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var cls = new AndroidJavaClass(JavaClass)) return cls.CallStatic<string>(method) ?? string.Empty;
            }
            catch (System.Exception e)
            {
                return e.Message;
            }
#else
            return string.Empty;
#endif
        }
    }
}
