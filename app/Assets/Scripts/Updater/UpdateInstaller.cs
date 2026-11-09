using UnityEngine;

namespace DiyVr.Updater
{
    /// <summary>
    /// Android calls for installing an APK: the "Install unknown apps" check and the package installer intent.
    /// The FileProvider and permission come from Plugins/Android/UpdaterRes.androidlib.
    /// </summary>
    public static class UpdateInstaller
    {
        private const int FlagGrantRead = 1;               // Intent.FLAG_GRANT_READ_URI_PERMISSION
        private const int FlagNewTask = 0x10000000;        // Intent.FLAG_ACTIVITY_NEW_TASK

        /// <summary>True when Android already lets this app install packages (always true in the Editor).</summary>
        public static bool CanInstall()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var activity = Activity())
                using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
                    return pm.Call<bool>("canRequestPackageInstalls");
            }
            catch (System.Exception e) { Debug.LogWarning("Updater: install check failed: " + e.Message); return false; }
#else
            return true;
#endif
        }

        /// <summary>Opens Android's "Install unknown apps" screen for this app.</summary>
        public static void OpenUnknownSourcesSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var activity = Activity())
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.MANAGE_UNKNOWN_APP_SOURCES", uri))
                {
                    intent.Call<AndroidJavaObject>("addFlags", FlagNewTask);
                    activity.Call("startActivity", intent);
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Updater: could not open settings: " + e.Message); }
#endif
        }

        /// <summary>Hands the downloaded APK to Android's package installer. Returns false if it could not start.</summary>
        public static bool Install(string apkPath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var activity = Activity())
                using (var file = new AndroidJavaObject("java.io.File", apkPath))
                using (var provider = new AndroidJavaClass("androidx.core.content.FileProvider"))
                using (var uri = provider.CallStatic<AndroidJavaObject>("getUriForFile", activity, Application.identifier + ".updater", file))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
                {
                    intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                    intent.Call<AndroidJavaObject>("addFlags", FlagGrantRead | FlagNewTask);
                    activity.Call("startActivity", intent);
                    return true;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Updater: installer start failed: " + e.Message); return false; }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject Activity()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                return player.GetStatic<AndroidJavaObject>("currentActivity");
        }
#endif
    }
}
