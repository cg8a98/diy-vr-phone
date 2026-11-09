using UnityEngine;

namespace DiyVr.Stereo
{
    /// <summary>
    /// Where head-locked panels sit. The panels share the test grid's depth (about 2 m) so the eyes do not have to
    /// refocus when a panel opens or closes. Pure maths so it can be tested.
    /// </summary>
    public static class PanelPlacement
    {
        /// <summary>Distance of the test grid (StereoTestContent), metres. Panels stay near this.</summary>
        public const float GridDistance = 2.5f;

        /// <summary>The lens tool panel's old placement (before it was moved to the grid's depth).</summary>
        public const float LensPanelOldDistance = 0.8f;
        public const float LensPanelOldWidth = 0.7f;

        /// <summary>Lens tool panel now: 2.0 m away, scaled to look the same size as before.</summary>
        public const float LensPanelDistance = 2.0f;
        public static readonly float LensPanelWidth = WidthForSameAngularSize(LensPanelOldWidth, LensPanelOldDistance, LensPanelDistance);

        /// <summary>Panel height divided by width (canvas 1000 x 640).</summary>
        public const float LensPanelAspect = 0.64f;

        /// <summary>The width that looks as wide as oldWidth did at oldDistance, when placed at newDistance.</summary>
        public static float WidthForSameAngularSize(float oldWidth, float oldDistance, float newDistance)
        {
            return oldWidth * newDistance / oldDistance;
        }

        /// <summary>Angle of the lens tool panel's bottom edge below straight ahead, degrees.</summary>
        public static float LensPanelBottomDegrees()
        {
            return Mathf.Atan(LensPanelWidth * LensPanelAspect * 0.5f / LensPanelDistance) * Mathf.Rad2Deg;
        }
    }
}
