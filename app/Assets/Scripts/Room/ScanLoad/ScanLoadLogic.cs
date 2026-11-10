using System.Globalization;
using System.Text;

namespace DiyVr.Room
{
    /// <summary>Pure rules for loading a room scan (no Unity types), so they can be tested without a phone.</summary>
    public static class ScanLoadLogic
    {
        /// <summary>Largest texture side kept in memory. An 8192 px texture would need about 256 MB on the GPU.</summary>
        public const int MaxTextureSize = 2048;
        public const string Extension = ".glb";
        public const int MaxNameLength = 60;
        /// <summary>Longest status label; a longer text is cut and ends in "...".</summary>
        public const int MaxLabelLength = 64;
        private const string FallbackName = "scan";

        /// <summary>True when either side is longer than <paramref name="max"/>.</summary>
        public static bool NeedsDownscale(int width, int height, int max)
        {
            return width > max || height > max;
        }

        /// <summary>
        /// Size to load a texture at: the longer side is cut to <paramref name="max"/>, the shape is kept, and a
        /// texture that already fits is never made bigger.
        /// </summary>
        public static void TextureTarget(int width, int height, int max, out int outWidth, out int outHeight)
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            if (max < 1) max = 1;
            if (!NeedsDownscale(width, height, max))
            {
                outWidth = width;
                outHeight = height;
                return;
            }
            if (width >= height)
            {
                outWidth = max;
                outHeight = (int)((long)height * max / width);
            }
            else
            {
                outHeight = max;
                outWidth = (int)((long)width * max / height);
            }
            if (outWidth < 1) outWidth = 1;
            if (outHeight < 1) outHeight = 1;
        }

        /// <summary>
        /// File name used inside the app's own storage for a picked file: no folders, only letters, digits,
        /// space, dash, underscore and dot, never empty, always ending in ".glb".
        /// </summary>
        public static string SafeFileName(string displayName)
        {
            string name = displayName ?? string.Empty;
            int slash = name.LastIndexOfAny(new[] { '/', '\\', ':' });
            if (slash >= 0) name = name.Substring(slash + 1);
            if (name.EndsWith(Extension, System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - Extension.Length);

            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                          || c == ' ' || c == '-' || c == '_' || c == '.';
                sb.Append(ok ? c : '_');
            }
            name = sb.ToString().Trim(' ', '.');
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength).Trim(' ', '.');
            if (name.Length == 0) name = FallbackName;
            return name + Extension;
        }

        /// <summary>
        /// True when the first bytes are a binary glTF 2 header: the letters "glTF" then version 2 (little endian).
        /// </summary>
        public static bool LooksLikeGlb(byte[] header, int count)
        {
            if (header == null || count < 12 || header.Length < 12) return false;
            if (header[0] != (byte)'g' || header[1] != (byte)'l' || header[2] != (byte)'T' || header[3] != (byte)'F') return false;
            return header[4] == 2 && header[5] == 0 && header[6] == 0 && header[7] == 0;
        }

        /// <summary>The label text for a loaded scan, for example "Scan: bedroom, 104212 tris, 2048px".</summary>
        public static string StatusText(string name, int triangles, int texturePx)
        {
            return "Scan: " + name + ", " + triangles.ToString(CultureInfo.InvariantCulture) + " tris, "
                   + texturePx.ToString(CultureInfo.InvariantCulture) + "px";
        }

        /// <summary>The label text for a failure, for example "Scan: not a .glb file".</summary>
        public static string ErrorText(string error)
        {
            return "Scan: " + (string.IsNullOrEmpty(error) ? "unknown error" : error);
        }

        /// <summary>One line, at most <paramref name="max"/> characters (the label does not wrap).</summary>
        public static string Shorten(string text, int max)
        {
            if (text == null) return string.Empty;
            text = text.Replace('\r', ' ').Replace('\n', ' ');
            if (max < 4 || text.Length <= max) return text;
            return text.Substring(0, max - 3) + "...";
        }
    }

    /// <summary>What the scan button did this frame.</summary>
    public enum ScanButtonEvent
    {
        None,
        /// <summary>Pressed and let go quickly: pick a scan file.</summary>
        ShortPress,
        /// <summary>Held down: hide or show the scan. Fires once, while the button is still down.</summary>
        LongPress,
    }

    /// <summary>
    /// One button, two jobs. A quick press fires <see cref="ScanButtonEvent.ShortPress"/> when the button is let go;
    /// holding it for <see cref="LongPressSeconds"/> fires <see cref="ScanButtonEvent.LongPress"/> once and then
    /// letting go does nothing.
    /// </summary>
    public sealed class ScanButtonLogic
    {
        public const float LongPressSeconds = 0.8f;

        private bool _down;
        private bool _longFired;
        private float _held;

        public ScanButtonEvent Update(bool pressed, float deltaTime)
        {
            if (pressed)
            {
                if (!_down)
                {
                    _down = true;
                    _longFired = false;
                    _held = 0f;
                    return ScanButtonEvent.None;
                }
                _held += deltaTime;
                if (!_longFired && _held >= LongPressSeconds)
                {
                    _longFired = true;
                    return ScanButtonEvent.LongPress;
                }
                return ScanButtonEvent.None;
            }
            if (!_down) return ScanButtonEvent.None;
            _down = false;
            return _longFired ? ScanButtonEvent.None : ScanButtonEvent.ShortPress;
        }
    }
}
