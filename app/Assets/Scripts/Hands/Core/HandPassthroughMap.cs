using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// Pure maths for drawing the hand skeleton ON the passthrough image. The passthrough is one mono camera image on a
    /// quad at a fixed distance in front of each eye camera (no disparity). A skeleton drawn in 3D at the hand's real
    /// distance has disparity, so the eyes cannot fuse it with the image and it looks blurred. Instead each landmark is
    /// put on the same plane, at the pixel where the camera saw it: it then sits exactly on the camera image of the
    /// hand, with the same (zero) disparity.
    /// </summary>
    public static class HandPassthroughMap
    {
        /// <summary>Distance the skeleton is drawn at, slightly in front of the image quad (0.12 m) so it is never hidden by it.</summary>
        public const float SkeletonDistance = 0.11f;
        /// <summary>The distance the world-space skeleton used to be drawn at; sizes are scaled to keep the same angular size.</summary>
        public const float ReferenceHandDistance = 0.4f;

        /// <summary>
        /// Point in an eye camera's local space (looks down +z) for a normalised landmark position (u, v: 0..1 across the
        /// camera image, v down). Same formula as the corners made by PassthroughLayout.IntrinsicsQuad, so the centre
        /// pixel maps to the quad centre and the image corners to the quad corners (when the whole image is shown).
        /// </summary>
        public static Vector3 PlanePoint(float u, float v, HandIntrinsics k, float distance)
        {
            return new Vector3((u * k.Width - k.Cx) / k.Fx * distance, (k.Cy - v * k.Height) / k.Fy * distance, distance);
        }

        /// <summary>Plane points for all landmarks (only x and y of each landmark are used).</summary>
        public static Vector3[] PlanePoints(Vector3[] landmarks, HandIntrinsics k, float distance)
        {
            var r = new Vector3[landmarks.Length];
            for (int i = 0; i < r.Length; i++) r[i] = PlanePoint(landmarks[i].x, landmarks[i].y, k, distance);
            return r;
        }

        /// <summary>Size at the plane that looks as big as <paramref name="sizeAtReference"/> metres at the reference distance.</summary>
        public static float ScaledSize(float sizeAtReference, float planeDistance = SkeletonDistance, float referenceDistance = ReferenceHandDistance)
        {
            return sizeAtReference * planeDistance / referenceDistance;
        }
    }

    /// <summary>Short look-ahead along the last observed velocity, to hide the delay of the 15 fps landmarks.</summary>
    public static class HandExtrapolator
    {
        public const float MinInterval = 0.005f;
        public const float MaxInterval = 0.2f;

        /// <summary>
        /// <paramref name="previous"/> and <paramref name="current"/> were observed <paramref name="interval"/> seconds apart;
        /// <paramref name="sinceCurrent"/> seconds have passed since <paramref name="current"/>. Returns current plus the
        /// velocity times the elapsed time, but never further than one interval ahead. An unusable interval returns current.
        /// </summary>
        public static Vector3 Predict(Vector3 previous, Vector3 current, float interval, float sinceCurrent)
        {
            if (interval < MinInterval || interval > MaxInterval || sinceCurrent <= 0f) return current;
            float ahead = Mathf.Min(sinceCurrent, interval);
            return current + (current - previous) * (ahead / interval);
        }
    }
}
