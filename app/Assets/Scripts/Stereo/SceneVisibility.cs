namespace DiyVr.Stereo
{
    /// <summary>
    /// The one rule for the test grid: while the camera view (passthrough) is on, the grid must not be shown,
    /// because the grid is opaque and would hide the camera image. The test cube and the lens tool panel are not
    /// affected by this rule.
    /// </summary>
    public static class SceneVisibility
    {
        /// <summary>The head-locked distortion grid shows only while the camera view is off.</summary>
        public static bool TestGridVisible(bool passthroughVisible)
        {
            return !passthroughVisible;
        }
    }
}
