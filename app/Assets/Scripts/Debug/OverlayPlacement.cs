using DiyVr.Stereo;
using UnityEngine;

namespace DiyVr.Diagnostics
{
    /// <summary>
    /// Where the head-locked debug panel sits, in the head (StereoRig) frame. Pure maths so it can be tested.
    /// Both panels sit near the test grid's depth (2 m) so the eyes do not refocus. The lens tool panel is centred
    /// straight ahead and its bottom edge is about 15.6 degrees below straight ahead (PanelPlacement);
    /// the debug panel must stay entirely below that.
    /// </summary>
    public static class OverlayPlacement
    {
        public const float Distance = 2.0f;
        public const float DegreesDown = 22f;
        public const float Width = 0.75f * 2.0f / 1.2f;      // same angular size as at 1.2 m
        public const float Height = 0.24f * 2.0f / 1.2f;

        /// <summary>Bottom edge of the lens tool panel, degrees below straight ahead.</summary>
        public static readonly float LensPanelBottomDegrees = PanelPlacement.LensPanelBottomDegrees();

        public static Vector3 LocalPosition(float distance, float degreesDown)
        {
            return new Vector3(0f, -Mathf.Tan(degreesDown * Mathf.Deg2Rad) * distance, distance);
        }

        /// <summary>Tilts the panel so it faces the eye (positive x rotation pitches the forward axis down).</summary>
        public static Quaternion LocalRotation(float degreesDown)
        {
            return Quaternion.Euler(degreesDown, 0f, 0f);
        }

        /// <summary>Angle below straight ahead of the panel's top edge, seen from the head, with the panel tilted to face it.</summary>
        public static float TopEdgeDegreesDown(float distance, float degreesDown, float height)
        {
            Vector3 centre = LocalPosition(distance, degreesDown);
            Vector3 up = LocalRotation(degreesDown) * Vector3.up;
            Vector3 top = centre + up * (height * 0.5f);
            return Mathf.Atan2(-top.y, top.z) * Mathf.Rad2Deg;
        }
    }
}
