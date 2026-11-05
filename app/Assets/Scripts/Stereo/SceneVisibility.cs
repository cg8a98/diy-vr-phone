namespace DiyVr.Stereo
{
    /// <summary>
    /// The one rule for the full-view test grids: while the camera view (passthrough) is on, no grid may be shown,
    /// because the grids are opaque and would hide the camera image. The test cube and the lens tool panel are not
    /// affected by this rule.
    /// </summary>
    public static class SceneVisibility
    {
        /// <summary>The head-locked distortion grid shows only while the camera view is off.</summary>
        public static bool TestGridVisible(bool passthroughVisible)
        {
            return !passthroughVisible;
        }

        /// <summary>The lens tool grid shows only while the tool is open and the camera view is off.</summary>
        public static bool LensGridVisible(bool passthroughVisible, bool lensToolOpen)
        {
            return lensToolOpen && !passthroughVisible;
        }
    }
}
