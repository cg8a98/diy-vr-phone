using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DiyVr.Stereo;
using UnityEngine;

namespace DiyVr.Stereo.LensTool
{
    /// <summary>
    /// Saves and loads named lens profiles as JSON files in one folder, and remembers the last used one.
    /// A profile's name is its file name; names are reduced to safe characters so a name can never point outside the folder.
    /// </summary>
    public sealed class LensProfileStore
    {
        public const string DefaultName = "generic-5-7in-viewer";
        public const string FolderName = "lens-profiles";
        public const string Extension = ".json";
        public const string LastUsedFile = "last-used.txt";
        public const int MaxNameLength = 48;

        private readonly IFileStore _fs;
        private readonly string _dir;

        public LensProfileStore(IFileStore fs, string directory)
        {
            if (fs == null) throw new ArgumentNullException("fs");
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("directory");
            _fs = fs;
            _dir = directory;
        }

        public string FolderPath { get { return _dir; } }

        /// <summary>
        /// Turns any text into a safe profile name: lower-case letters, digits, '-' and '_' only, no leading or trailing dashes.
        /// Returns null if nothing usable is left.
        /// </summary>
        public static string SafeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var sb = new StringBuilder();
            bool lastDash = false;
            foreach (char raw in name.Trim())
            {
                char c = char.ToLowerInvariant(raw);
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (ok) { sb.Append(c); lastDash = false; }
                else if (!lastDash) { sb.Append('-'); lastDash = true; }
            }
            var s = sb.ToString().Trim('-');
            if (s.Length > MaxNameLength) s = s.Substring(0, MaxNameLength).Trim('-');
            return s.Length == 0 ? null : s;
        }

        private string PathFor(string safe) { return Path.Combine(_dir, safe + Extension); }

        public bool Exists(string name)
        {
            var s = SafeName(name);
            return s != null && _fs.Exists(PathFor(s));
        }

        /// <summary>Saves the profile; returns the name it was saved under. Throws ArgumentException for an unusable name.</summary>
        public string Save(string name, LensProfile profile)
        {
            var s = SafeName(name);
            if (s == null) throw new ArgumentException("Profile name has no usable characters.");
            _fs.WriteAllText(PathFor(s), profile.ToJson());
            return s;
        }

        /// <summary>Copies the saved profile into <paramref name="into"/>. Returns false (and leaves it unchanged) if missing or unreadable.</summary>
        public bool TryLoad(string name, LensProfile into)
        {
            var s = SafeName(name);
            if (s == null) return false;
            var path = PathFor(s);
            LensProfile temp = null;
            try
            {
                if (!_fs.Exists(path)) return false;
                temp = LensProfile.CreateDefault();
                temp.FromJson(_fs.ReadAllText(path));
                LensParameters.ClampAll(temp);
                into.FromJson(temp.ToJson());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (temp != null) DestroyObject(temp);
            }
        }

        /// <summary>Saved profile names, sorted.</summary>
        public List<string> List()
        {
            var names = new List<string>();
            foreach (var f in _fs.ListFileNames(_dir, Extension))
            {
                var stem = f.Substring(0, f.Length - Extension.Length);
                if (SafeName(stem) == stem) names.Add(stem);
            }
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        public string NextFreeName(string prefix)
        {
            var p = SafeName(prefix) ?? "headset";
            for (int i = 1; i < 10000; i++)
            {
                var candidate = p + "-" + i;
                if (!Exists(candidate)) return candidate;
            }
            return p + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        /// <summary>Name of the last used profile, or null if none is remembered or its file is gone.</summary>
        public string LastUsed
        {
            get
            {
                try
                {
                    var path = Path.Combine(_dir, LastUsedFile);
                    if (!_fs.Exists(path)) return null;
                    var s = SafeName(_fs.ReadAllText(path));
                    return s != null && Exists(s) ? s : null;
                }
                catch (Exception) { return null; }
            }
        }

        public void SetLastUsed(string name)
        {
            var s = SafeName(name);
            if (s == null) return;
            _fs.WriteAllText(Path.Combine(_dir, LastUsedFile), s);
        }

        /// <summary>Writes the shipped default profile if it is not there yet.</summary>
        public void EnsureDefault()
        {
            if (Exists(DefaultName)) return;
            var d = LensProfile.CreateDefault();
            try { Save(DefaultName, d); }
            finally { DestroyObject(d); }
        }

        /// <summary>Loads the last used profile, or the default. Returns the name that was loaded.</summary>
        public string LoadStartup(LensProfile into)
        {
            EnsureDefault();
            var last = LastUsed;
            if (last != null && TryLoad(last, into)) return last;
            if (!TryLoad(DefaultName, into))
            {
                // Default file unreadable: rewrite it from the built-in values.
                var d = LensProfile.CreateDefault();
                try { into.FromJson(d.ToJson()); Save(DefaultName, d); }
                finally { DestroyObject(d); }
            }
            return DefaultName;
        }

        private static void DestroyObject(UnityEngine.Object o)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
