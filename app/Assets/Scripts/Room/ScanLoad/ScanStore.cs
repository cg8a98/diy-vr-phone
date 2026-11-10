using System;
using System.IO;

namespace DiyVr.Room
{
    /// <summary>
    /// The app's own scan folder (inside its private storage, never in git and never uploaded): where a picked
    /// file is copied to, and which scan was used last so it loads by itself on the next start.
    /// </summary>
    public sealed class ScanStore
    {
        public const string FolderName = "scans";
        public const string LastFileName = "last-scan.txt";
        public const string IncomingFileName = "incoming.tmp";

        private readonly string _dir;

        /// <param name="rootDir">The app's private data folder (Application.persistentDataPath on the phone).</param>
        public ScanStore(string rootDir)
        {
            _dir = Path.Combine(rootDir, FolderName);
        }

        public string DirectoryPath { get { return _dir; } }

        /// <summary>Where the file picker writes the chosen file before it is checked.</summary>
        public string IncomingPath { get { return Path.Combine(_dir, IncomingFileName); } }

        /// <summary>Makes the scan folder and removes a half-copied file from an earlier try.</summary>
        public void PrepareIncoming()
        {
            Directory.CreateDirectory(_dir);
            if (File.Exists(IncomingPath)) File.Delete(IncomingPath);
        }

        /// <summary>
        /// Checks the freshly copied file and, if it is a binary glTF, keeps it as scans/&lt;safe name&gt;.glb
        /// (replacing an older scan with the same name) and remembers it as the last scan.
        /// A file that is not a .glb is deleted.
        /// </summary>
        public bool TryImport(string displayName, out string scanPath, out string error)
        {
            scanPath = null;
            error = null;
            try
            {
                string incoming = IncomingPath;
                if (!File.Exists(incoming))
                {
                    error = "no file was copied";
                    return false;
                }
                if (!LooksLikeGlbFile(incoming))
                {
                    File.Delete(incoming);
                    error = "not a .glb file";
                    return false;
                }
                string fileName = ScanLoadLogic.SafeFileName(displayName);
                string dest = Path.Combine(_dir, fileName);
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(incoming, dest);
                RememberLast(fileName);
                scanPath = dest;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>Full path of the scan used last, or null when there is none (or its file is gone).</summary>
        public string LastScanPath()
        {
            try
            {
                string marker = Path.Combine(_dir, LastFileName);
                if (!File.Exists(marker)) return null;
                // Only a bare file name is trusted, so the marker can never point outside the scan folder.
                string fileName = Path.GetFileName(File.ReadAllText(marker).Trim());
                if (string.IsNullOrEmpty(fileName)) return null;
                string path = Path.Combine(_dir, fileName);
                return File.Exists(path) ? path : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RememberLast(string fileName)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, LastFileName), Path.GetFileName(fileName));
        }

        /// <summary>The name shown to the owner: the file name without ".glb".</summary>
        public static string DisplayName(string scanPath)
        {
            return Path.GetFileNameWithoutExtension(scanPath);
        }

        private static bool LooksLikeGlbFile(string path)
        {
            var header = new byte[12];
            int read = 0;
            using (var fs = File.OpenRead(path))
            {
                while (read < header.Length)
                {
                    int n = fs.Read(header, read, header.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
            }
            return ScanLoadLogic.LooksLikeGlb(header, read);
        }
    }
}
