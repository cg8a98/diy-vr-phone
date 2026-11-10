using UnityEngine;

namespace DiyVr.Room
{
    /// <summary>Makes a smaller copy of a texture on the GPU (the scan's 8K texture becomes 2K).</summary>
    public static class ScanTextureScaler
    {
        /// <summary>
        /// Returns a new texture whose longer side is at most <paramref name="max"/> (same shape, with mipmaps).
        /// The caller owns the result and still owns <paramref name="source"/>.
        /// </summary>
        public static Texture2D Downscale(Texture2D source, int max)
        {
            int width, height;
            ScanLoadLogic.TextureTarget(source.width, source.height, max, out width, out height);

            // Halve step by step so every step averages 2x2 pixels; one big jump would skip pixels and shimmer.
            Texture current = source;
            RenderTexture held = null;
            int cw = source.width, ch = source.height;
            do
            {
                int nw = Mathf.Max(width, cw / 2);
                int nh = Mathf.Max(height, ch / 2);
                RenderTexture rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                rt.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, rt);
                if (held != null) RenderTexture.ReleaseTemporary(held);
                held = rt;
                current = rt;
                cw = nw;
                ch = nh;
            } while (cw > width || ch > height);

            RenderTexture before = RenderTexture.active;
            RenderTexture.active = held;
            var result = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            result.Apply(true, true); // build mipmaps, then drop the CPU copy
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(held);

            result.name = source.name + " (" + Mathf.Max(width, height) + "px)";
            result.wrapMode = source.wrapMode;
            result.filterMode = FilterMode.Trilinear;
            result.anisoLevel = 2;
            return result;
        }
    }
}
