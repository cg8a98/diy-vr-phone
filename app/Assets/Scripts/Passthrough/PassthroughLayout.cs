using UnityEngine;

namespace DiyVr.Passthrough
{
    /// <summary>Pure geometry for placing the camera image in front of an eye camera.</summary>
    public static class PassthroughLayout
    {
        /// <summary>
        /// Corners (bottom-left, bottom-right, top-right, top-left) in the eye camera's local space of the
        /// rectangle that exactly fills the eye frustum at <paramref name="distance"/> metres in front of it.
        /// Works for asymmetric frustums (the lens-corrected eyes use them).
        /// </summary>
        public static Vector3[] FrustumQuad(Matrix4x4 projection, float distance)
        {
            Matrix4x4 inv = projection.inverse;
            float[] xs = { -1f, 1f, 1f, -1f };
            float[] ys = { -1f, -1f, 1f, 1f };
            var result = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = inv.MultiplyPoint(new Vector3(xs[i], ys[i], 1f)); // view space, looks down -z
                float k = distance / -p.z;
                result[i] = new Vector3(p.x * k, p.y * k, distance);          // camera local space looks down +z
            }
            return result;
        }

        /// <summary>
        /// Centre crop of the camera image so it fills a quad of the given aspect without stretching.
        /// Returns (uMin, vMin, uWidth, vHeight).
        /// </summary>
        public static Rect CenterCrop(float quadAspect, float imageAspect)
        {
            if (quadAspect <= 0f || imageAspect <= 0f) return new Rect(0f, 0f, 1f, 1f);
            if (quadAspect < imageAspect)
            {
                float w = quadAspect / imageAspect;
                return new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            float h = imageAspect / quadAspect;
            return new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }
    }
}
