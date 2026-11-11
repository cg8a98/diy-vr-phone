using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// The saved scan-in-room poses, one per scan file, in one small JSON file in the app's private storage on
    /// the phone. Never leaves the phone and is never written to the log.
    /// </summary>
    public sealed class ScanAlignStore
    {
        public const string FileName = "scan-align.json";
        private const int MaxKeyLength = 80;

        [Serializable]
        public sealed class Entry
        {
            public string key;
            public float x, y, z;
            public float yaw;
        }

        [Serializable]
        public sealed class FileData
        {
            public int version = 1;
            public List<Entry> scans = new List<Entry>();
        }

        private readonly string _path;
        private FileData _data = new FileData();

        public ScanAlignStore(string path)
        {
            _path = path;
            Load();
        }

        public string FilePath { get { return _path; } }
        public int Count { get { return _data.scans.Count; } }

        /// <summary>Name used to find a scan's pose: the scan's file name with anything unusual replaced.</summary>
        public static string SafeKey(string scanName)
        {
            if (string.IsNullOrEmpty(scanName)) return "scan";
            var chars = new char[Math.Min(scanName.Length, MaxKeyLength)];
            for (int i = 0; i < chars.Length; i++)
            {
                char c = char.ToLowerInvariant(scanName[i]);
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.';
                chars[i] = ok ? c : '_';
            }
            return new string(chars);
        }

        public bool TryGet(string key, out ScanPose pose)
        {
            Entry e = Find(key);
            if (e == null)
            {
                pose = new ScanPose(Vector3.zero, 0f);
                return false;
            }
            pose = new ScanPose(new Vector3(e.x, e.y, e.z), e.yaw);
            return true;
        }

        /// <summary>Remembers the pose for this scan and writes the file. False (with the reason) if writing failed.</summary>
        public bool Set(string key, ScanPose pose, out string error)
        {
            Entry e = Find(key);
            if (e == null)
            {
                e = new Entry { key = key };
                _data.scans.Add(e);
            }
            e.x = pose.Position.x;
            e.y = pose.Position.y;
            e.z = pose.Position.z;
            e.yaw = pose.YawDegrees;
            try
            {
                string dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_path, JsonUtility.ToJson(_data, true));
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private Entry Find(string key)
        {
            for (int i = 0; i < _data.scans.Count; i++)
                if (_data.scans[i] != null && _data.scans[i].key == key) return _data.scans[i];
            return null;
        }

        // A missing or damaged file just means "nothing aligned yet".
        private void Load()
        {
            _data = new FileData();
            try
            {
                if (!File.Exists(_path)) return;
                FileData read = JsonUtility.FromJson<FileData>(File.ReadAllText(_path));
                if (read != null && read.scans != null) _data = read;
            }
            catch (Exception)
            {
                _data = new FileData();
            }
        }
    }
}
