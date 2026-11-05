using UnityEngine;

namespace DiyVr.Core
{
    /// <summary>Pure maths: the field of view of the phone camera image, from its intrinsics.</summary>
    public static class CameraFov
    {
        /// <summary>
        /// Horizontal and vertical field of view in degrees. fx, fy: focal length in pixels; cx, cy: principal point
        /// in pixels; w, h: image size in pixels. Returns (0, 0) when the input is unusable.
        /// </summary>
        public static Vector2 Degrees(float fx, float fy, float cx, float cy, float w, float h)
        {
            if (fx <= 0f || fy <= 0f || w <= 0f || h <= 0f) return Vector2.zero;
            float hor = Mathf.Atan(cx / fx) + Mathf.Atan((w - cx) / fx);
            float ver = Mathf.Atan(cy / fy) + Mathf.Atan((h - cy) / fy);
            return new Vector2(hor * Mathf.Rad2Deg, ver * Mathf.Rad2Deg);
        }
    }
}
