using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>
    /// The two printed wall markers. The pictures are our own (made by hardware/room-markers/make_markers.py) and are
    /// stored as PNG bytes in Resources, so Unity does not compress them: ARCore needs a plain, readable texture.
    /// </summary>
    public static class RoomMarkers
    {
        /// <summary>Printed width of each marker, black edge to black edge: 15 cm.</summary>
        public const float WidthMeters = 0.15f;
        public const int Count = 2;
        public const int PixelSize = 1024;

        private static readonly string[] Names = { "room-marker-a", "room-marker-b" };
        private static readonly string[] Letters = { "A", "B" };
        private static readonly string[] ResourcePaths = { "RoomFrame/marker_a", "RoomFrame/marker_b" };

        /// <summary>The name ARCore reports for a marker (0 = A, 1 = B).</summary>
        public static string Name(int index) { return Names[index]; }

        public static string Letter(int index) { return Letters[index]; }

        /// <summary>0 for A, 1 for B, -1 for anything else.</summary>
        public static int IndexOf(string name)
        {
            for (int i = 0; i < Names.Length; i++)
                if (Names[i] == name) return i;
            return -1;
        }

        /// <summary>
        /// Loads a marker picture as a readable RGBA32 texture (the caller destroys it when done). Null if the file is
        /// missing or broken.
        /// </summary>
        public static Texture2D LoadTexture(int index)
        {
            var asset = Resources.Load<TextAsset>(ResourcePaths[index]);
            if (asset == null) return null;
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!decoded.LoadImage(asset.bytes, false))
            {
                DestroyNow(decoded);
                return null;
            }
            // LoadImage picks its own format; copy into RGBA32, which ARCore accepts for certain.
            var tex = new Texture2D(decoded.width, decoded.height, TextureFormat.RGBA32, false);
            tex.name = Names[index];
            tex.SetPixels32(decoded.GetPixels32());
            tex.Apply(false, false);
            DestroyNow(decoded);
            return tex;
        }

        /// <summary>Destroy that also works in EditMode tests.</summary>
        public static void DestroyNow(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
