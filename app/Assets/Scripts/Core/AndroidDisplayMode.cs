using UnityEngine;

namespace DiyVr.Core
{
    /// <summary>
    /// Asks Android (through the activity window) for the highest refresh-rate display mode at the
    /// current panel size, so Samsung's Adaptive refresh does not drop us to 60 Hz.
    /// </summary>
    public static class AndroidDisplayMode
    {
        /// <summary>Display mode id last requested; -1 if none.</summary>
        public static int RequestedModeId { get; private set; } = -1;

        /// <summary>Refresh rate (Hz) of the last requested mode; 0 if none.</summary>
        public static float RequestedHz { get; private set; }

        /// <summary>Last error message, or null/empty when all is well.</summary>
        public static string LastError { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
        public static void Request()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    var display = GetDisplay(activity);
                    var modes = display.Call<AndroidJavaObject[]>("getSupportedModes");
                    var current = display.Call<AndroidJavaObject>("getMode");
                    int curW = current.Call<int>("getPhysicalWidth");
                    int curH = current.Call<int>("getPhysicalHeight");

                    int n = modes.Length;
                    var ids = new int[n];
                    var w = new int[n];
                    var h = new int[n];
                    var hz = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        ids[i] = modes[i].Call<int>("getModeId");
                        w[i] = modes[i].Call<int>("getPhysicalWidth");
                        h[i] = modes[i].Call<int>("getPhysicalHeight");
                        hz[i] = modes[i].Call<float>("getRefreshRate");
                    }

                    int pick = DisplayModeChooser.ChooseIndex(ids, w, h, hz, curW, curH);
                    if (pick < 0)
                    {
                        LastError = "no display modes";
                        return;
                    }

                    int modeId = ids[pick];
                    float rate = hz[pick];
                    RequestedModeId = modeId;
                    RequestedHz = rate;

                    activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                    {
                        try
                        {
                            var window = activity.Call<AndroidJavaObject>("getWindow");
                            var attrs = window.Call<AndroidJavaObject>("getAttributes");
                            attrs.Set<int>("preferredDisplayModeId", modeId);
                            attrs.Set<float>("preferredRefreshRate", rate);
                            window.Call("setAttributes", attrs);
                            LastError = null;
                        }
                        catch (System.Exception e)
                        {
                            LastError = e.Message;
                            Debug.LogWarning("Display mode request failed on UI thread: " + e.Message);
                        }
                    }));
                }
            }
            catch (System.Exception e)
            {
                LastError = e.Message;
                Debug.LogWarning("Display mode request failed: " + e.Message);
            }
        }

        private static AndroidJavaObject GetDisplay(AndroidJavaObject activity)
        {
            try
            {
                // API 30+.
                var d = activity.Call<AndroidJavaObject>("getDisplay");
                if (d != null) return d;
            }
            catch (System.Exception)
            {
                // Fall through to the older route.
            }
            using (var wm = activity.Call<AndroidJavaObject>("getWindowManager"))
            {
                return wm.Call<AndroidJavaObject>("getDefaultDisplay");
            }
        }
#else
        /// <summary>No-op outside an Android player build.</summary>
        public static void Request()
        {
        }
#endif
    }
}
