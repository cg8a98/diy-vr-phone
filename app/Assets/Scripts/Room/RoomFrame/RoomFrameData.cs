using System;
using System.IO;
using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// What is saved on the phone (one small JSON file in the app's private folder). Private data: it never goes into
    /// git and is never written to the log or sent anywhere.
    /// </summary>
    [Serializable]
    public sealed class RoomFrameData
    {
        public const int CurrentVersion = 1;
        public const string FileName = "room-frame.json";

        public int version = CurrentVersion;
        /// <summary>True once "calibrate" has run.</summary>
        public bool calibrated;
        /// <summary>Printed marker width the calibration was made with, metres.</summary>
        public float markerWidth;
        /// <summary>Where marker B was at calibration, in room coordinates. Later starts compare against it.</summary>
        public Vector3 markerBInRoom;
        /// <summary>True if ARCore had found the floor at calibration.</summary>
        public bool hasFloor;
        /// <summary>Floor height in room coordinates (negative: the floor is below marker A).</summary>
        public float floorY;
        public bool hasTestObject;
        /// <summary>The test object's position in room coordinates.</summary>
        public Vector3 testObject;
        public string savedUtc = string.Empty;

        public string ToJson() { return JsonUtility.ToJson(this, true); }

        /// <summary>Reads the JSON text. Returns null if it is empty, broken or from a newer app version.</summary>
        public static RoomFrameData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<RoomFrameData>(json);
                if (data == null || data.version < 1 || data.version > CurrentVersion) return null;
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Loads the file; a missing or unreadable file gives fresh, uncalibrated data.</summary>
        public static RoomFrameData Load(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var data = FromJson(File.ReadAllText(path));
                    if (data != null) return data;
                }
            }
            catch (Exception e)
            {
                // The kind of error only: never the file's contents.
                UnityEngine.Debug.LogWarning("Room frame: could not read the saved file (" + e.GetType().Name + ").");
            }
            return new RoomFrameData();
        }

        /// <summary>Writes the file (to a temporary file first, so a crash cannot leave half a file). False on failure.</summary>
        public bool Save(string path)
        {
            try
            {
                savedUtc = DateTime.UtcNow.ToString("o");
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, ToJson());
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Room frame: could not save (" + e.GetType().Name + ").");
                return false;
            }
        }
    }
}
