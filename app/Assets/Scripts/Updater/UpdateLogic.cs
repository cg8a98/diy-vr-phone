using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiyVr.Updater
{
    /// <summary>Fields of GitHub's "latest release" JSON that the updater uses (names match the JSON for JsonUtility).</summary>
    [Serializable]
    public class ReleaseAssetDto
    {
        public string name;
        public string browser_download_url;
        public long size;
    }

    [Serializable]
    public class ReleaseDto
    {
        public string tag_name;
        public List<ReleaseAssetDto> assets;
    }

    /// <summary>A release the app can install.</summary>
    public sealed class UpdateOffer
    {
        public string Version;      // e.g. "0.1.81"
        public string Url;
        public long Size;
    }

    /// <summary>A version like 0.1.80 (2 to 4 numeric parts). Comparable.</summary>
    public readonly struct ReleaseVersion : IComparable<ReleaseVersion>
    {
        private readonly int[] _parts; // always length 4

        private ReleaseVersion(int[] parts) { _parts = parts; }

        /// <summary>Accepts "v0.1.80" or "0.1.80". Returns false for anything else.</summary>
        public static bool TryParse(string text, out ReleaseVersion version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string s = text.Trim();
            if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V')) s = s.Substring(1);
            string[] bits = s.Split('.');
            if (bits.Length < 2 || bits.Length > 4) return false;
            var parts = new int[4];
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i].Length == 0 || bits[i].Length > 9) return false;
                for (int c = 0; c < bits[i].Length; c++)
                    if (bits[i][c] < '0' || bits[i][c] > '9') return false;
                parts[i] = int.Parse(bits[i], System.Globalization.CultureInfo.InvariantCulture);
            }
            version = new ReleaseVersion(parts);
            return true;
        }

        public int CompareTo(ReleaseVersion other)
        {
            for (int i = 0; i < 4; i++)
            {
                int c = _parts[i].CompareTo(other._parts[i]);
                if (c != 0) return c;
            }
            return 0;
        }

        public override string ToString()
        {
            if (_parts == null) return "?";
            int n = 4;
            while (n > 2 && _parts[n - 1] == 0) n--;
            return string.Join(".", _parts, 0, n);
        }
    }

    /// <summary>Pure update decisions: version compare and choosing the APK from a release.</summary>
    public static class UpdateLogic
    {
        /// <summary>Only APKs published by this repository's releases are ever downloaded.</summary>
        public const string DownloadPrefix = "https://github.com/cg8a98/diy-vr-phone/releases/download/";

        public static bool IsNewer(string current, string latest)
        {
            ReleaseVersion c, l;
            if (!ReleaseVersion.TryParse(current, out c) || !ReleaseVersion.TryParse(latest, out l)) return false;
            return l.CompareTo(c) > 0;
        }

        /// <summary>Returns the first .apk asset with a plausible URL and size, or null.</summary>
        public static ReleaseAssetDto PickApk(ReleaseDto release)
        {
            if (release == null || release.assets == null) return null;
            for (int i = 0; i < release.assets.Count; i++)
            {
                var a = release.assets[i];
                if (a == null || string.IsNullOrEmpty(a.name) || string.IsNullOrEmpty(a.browser_download_url)) continue;
                if (!a.name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) continue;
                if (a.size <= 0) continue;
                if (!a.browser_download_url.StartsWith(DownloadPrefix, StringComparison.Ordinal)) continue;
                return a;
            }
            return null;
        }

        /// <summary>
        /// Reads the latest-release JSON. Returns an offer only when the tag parses, is newer than
        /// <paramref name="currentVersion"/> and the release has an installable APK.
        /// </summary>
        public static UpdateOffer TryGetOffer(string json, string currentVersion)
        {
            if (string.IsNullOrEmpty(json)) return null;
            ReleaseDto release;
            try { release = JsonUtility.FromJson<ReleaseDto>(json); }
            catch (Exception) { return null; }
            if (release == null) return null;

            ReleaseVersion latest;
            if (!ReleaseVersion.TryParse(release.tag_name, out latest)) return null;
            if (!IsNewer(currentVersion, release.tag_name)) return null;
            var apk = PickApk(release);
            if (apk == null) return null;
            return new UpdateOffer { Version = latest.ToString(), Url = apk.browser_download_url, Size = apk.size };
        }

        public static string OfferText(string version)
        {
            return "Update v" + version + " available — A: install, B: later\n(or tap the screen to install)";
        }
    }
}
