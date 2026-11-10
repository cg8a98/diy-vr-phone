using UnityEngine;

namespace DiyVr.Core
{
    /// <summary>
    /// Set by a feature that takes over the whole controller for a while (scan align mode, ticket 3.3).
    /// While <see cref="IsOpen"/> is true GamepadControls ignores every button, and keeps ignoring A and B until
    /// they are released, so "A = save" in that feature does not also move the test cube.
    /// </summary>
    public static class ControllerModal
    {
        public static bool IsOpen { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsOpen = false;
        }
    }
}
