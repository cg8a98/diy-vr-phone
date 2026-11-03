using System;
using System.Collections.Generic;
using System.IO;

namespace DiyVr.Stereo.LensTool
{
    /// <summary>Minimal file access so the profile logic can be tested without touching the disk.</summary>
    public interface IFileStore
    {
        bool Exists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string text);
        /// <summary>File names (without folder) in a folder that end with the given extension, e.g. ".json".</summary>
        IList<string> ListFileNames(string directory, string extension);
    }

    public sealed class PhysicalFileStore : IFileStore
    {
        public bool Exists(string path) { return File.Exists(path); }

        public string ReadAllText(string path) { return File.ReadAllText(path); }

        public void WriteAllText(string path, string text)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            // Write to a temp file first so a crash mid-save cannot destroy the old profile.
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public IList<string> ListFileNames(string directory, string extension)
        {
            var result = new List<string>();
            if (!Directory.Exists(directory)) return result;
            foreach (var f in Directory.GetFiles(directory))
            {
                var n = Path.GetFileName(f);
                if (n.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) result.Add(n);
            }
            return result;
        }
    }
}
