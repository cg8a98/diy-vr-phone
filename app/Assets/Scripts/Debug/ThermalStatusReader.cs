using UnityEngine;

namespace DiyVr.Diagnostics
{
    /// <summary>
    /// Reads Android's thermal status (PowerManager.getCurrentThermalStatus, API 29+).
    /// Kept separate from the overlay so the thermal governor (ticket 7) can reuse it.
    /// </summary>
    public sealed class ThermalStatusReader
    {
        public const int MinApiLevel = 29;

        /// <summary>Returned by <see cref="Read"/> when the status cannot be read (editor, old Android, error).</summary>
        public const int Unavailable = -1;

        public const string UnavailableLabel = "n/a";

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _powerManager;
        private bool _initialised;
#endif

        /// <summary>Maps an Android thermal status code (0 None ... 6 Shutdown) to a label.</summary>
        public static string ToLabel(int status)
        {
            switch (status)
            {
                case 0: return "None";
                case 1: return "Light";
                case 2: return "Moderate";
                case 3: return "Severe";
                case 4: return "Critical";
                case 5: return "Emergency";
                case 6: return "Shutdown";
                default: return UnavailableLabel;
            }
        }

        /// <summary>Current status code 0-6, or <see cref="Unavailable"/>.</summary>
        public int Read()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!_initialised)
                {
                    _initialised = true;
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        if (version.GetStatic<int>("SDK_INT") < MinApiLevel)
                            return Unavailable;
                    }

                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        _powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
                    }
                }

                if (_powerManager == null)
                    return Unavailable;

                return _powerManager.Call<int>("getCurrentThermalStatus");
            }
            catch (System.Exception)
            {
                return Unavailable;
            }
#else
            return Unavailable;
#endif
        }

        /// <summary>Current status as a label, or "n/a".</summary>
        public string ReadLabel()
        {
            return ToLabel(Read());
        }
    }
}
