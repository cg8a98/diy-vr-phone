using UnityEngine;

namespace DiyVr.Passthrough
{
    /// <summary>Pure geometry for placing the camera image in front of an eye camera.</summary>
    public static class PassthroughLayout
    {
        /// <summary>Layer of the left eye's image quad. (Layer 31 is the stereo compositor's.)</summary>
        public const int LeftLayer = 29;
        /// <summary>Layer of the right eye's image quad.</summary>
        public const int RightLayer = 30;

        /// <summary>
        /// Culling mask for an eye camera: the base mask minus the OTHER eye's quad layer, so each eye only
        /// ever sees its own passthrough quad (the quads sit close to the eyes, which are only 6 cm apart).
        /// </summary>
        public static int EyeCullingMask(int baseMask, bool leftEye)
        {
            int own = 1 << (leftEye ? LeftLayer : RightLayer);
            int other = 1 << (leftEye ? RightLayer : LeftLayer);
            return (baseMask | own) & ~other;
        }

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

        /// <summary>Default field of view used until the camera intrinsics are known.</summary>
        public const float FallbackHorizontalFovDegrees = 70f;
        public const float FallbackVerticalFovDegrees = 55f;

        /// <summary>
        /// Corners (bottom-left, bottom-right, top-right, top-left) in the eye camera's local space (looks down +z) of
        /// a rectangle at <paramref name="distance"/> that shows the camera image at its true angular size. The
        /// optical axis (the principal point) lands straight ahead of the eye, not at the frustum centre.
        /// fx, fy, cx, cy, w, h are camera intrinsics in pixels (cy measured from the top of the image).
        /// <paramref name="visible"/> is the part of the image that is shown, in 0..1 image coordinates with v up
        /// (default: the whole image).
        /// </summary>
        public static Vector3[] IntrinsicsQuad(float fx, float fy, float cx, float cy, float w, float h, float distance, Rect visible)
        {
            float u0 = visible.xMin * w, u1 = visible.xMax * w;
            float pyTop = (1f - visible.yMax) * h, pyBottom = (1f - visible.yMin) * h;
            float xl = (u0 - cx) / fx * distance, xr = (u1 - cx) / fx * distance;
            float yt = (cy - pyTop) / fy * distance, yb = (cy - pyBottom) / fy * distance;
            return new[]
            {
                new Vector3(xl, yb, distance), new Vector3(xr, yb, distance),
                new Vector3(xr, yt, distance), new Vector3(xl, yt, distance)
            };
        }

        /// <summary>The whole camera image at its true angular size. See the overload with a visible rectangle.</summary>
        public static Vector3[] IntrinsicsQuad(float fx, float fy, float cx, float cy, float w, float h, float distance)
        {
            return IntrinsicsQuad(fx, fy, cx, cy, w, h, distance, new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Symmetric quad for a given field of view (used until the real intrinsics are known).</summary>
        public static Vector3[] SymmetricQuad(float horizontalFovDegrees, float verticalFovDegrees, float distance)
        {
            float hx = Mathf.Tan(horizontalFovDegrees * 0.5f * Mathf.Deg2Rad) * distance;
            float hy = Mathf.Tan(verticalFovDegrees * 0.5f * Mathf.Deg2Rad) * distance;
            return new[]
            {
                new Vector3(-hx, -hy, distance), new Vector3(hx, -hy, distance),
                new Vector3(hx, hy, distance), new Vector3(-hx, hy, distance)
            };
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
