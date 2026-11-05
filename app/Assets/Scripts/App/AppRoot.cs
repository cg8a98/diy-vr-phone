using DiyVr.Diagnostics;
using DiyVr.Passthrough;
using DiyVr.Stereo;
using DiyVr.Stereo.LensTool;
using DiyVr.Tracking;
using UnityEngine;

namespace DiyVr.App
{
    /// <summary>
    /// Composition root: builds the whole milestone 1 app in code when the first scene has loaded, so the scene
    /// file stays almost empty. Everything is added only if missing, so a scene that already holds its own
    /// StereoRig (StereoTest) still works, and the per-feature auto-installers find nothing left to do.
    ///
    /// Built objects: StereoRig + HeadPoseDriver + PassthroughController + LensToolController + GamepadControls on one "VR Rig"
    /// object, plus StereoTestContent and the DebugOverlay.
    /// Touch gestures are shared through <see cref="DiyVr.Core.GestureArbiter"/>.
    /// </summary>
    public static class AppRoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            DiyVr.Core.GestureArbiter.Shared.LensToolOpen = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            var rig = Object.FindFirstObjectByType<StereoRig>();
            if (rig == null)
            {
                // Inactive while assembling so every component sees its final setup in OnEnable/Start.
                var go = new GameObject("VR Rig");
                go.SetActive(false);
                rig = go.AddComponent<StereoRig>();
                go.AddComponent<HeadPoseDriver>();
                go.AddComponent<PassthroughController>();
                go.AddComponent<LensToolController>().rig = rig;
                go.SetActive(true);
            }
            else
            {
                AddIfMissing<HeadPoseDriver>(rig.gameObject);
                AddIfMissing<PassthroughController>(rig.gameObject);
                var lens = Object.FindFirstObjectByType<LensToolController>();
                if (lens == null) rig.gameObject.AddComponent<LensToolController>().rig = rig;
            }

            if (Object.FindFirstObjectByType<StereoTestContent>() == null)
            {
                var content = new GameObject("Stereo Test Content");
                content.AddComponent<StereoTestContent>().head = rig.transform;
            }

            if (Object.FindFirstObjectByType<DebugOverlay>() == null)
                new GameObject("Debug Overlay").AddComponent<DebugOverlay>();

            // While the camera view is on, hide the opaque test grids so the camera image is not covered.
            var passthrough = Object.FindFirstObjectByType<PassthroughController>();
            var lensTool = Object.FindFirstObjectByType<LensToolController>();
            var testContent = Object.FindFirstObjectByType<StereoTestContent>();
            if (passthrough != null)
            {
                if (testContent != null) testContent.SetGridVisible(SceneVisibility.TestGridVisible(passthrough.IsVisible));
                if (lensTool != null) lensTool.SetGridSuppressed(passthrough.IsVisible);
                passthrough.VisibilityChanged += visible =>
                {
                    if (testContent != null) testContent.SetGridVisible(SceneVisibility.TestGridVisible(visible));
                    if (lensTool != null) lensTool.SetGridSuppressed(visible);
                };
            }

            // Xbox controller (Bluetooth). Added last so every component it points at exists.
            var pad = rig.gameObject.GetComponent<GamepadControls>();
            if (pad == null) pad = rig.gameObject.AddComponent<GamepadControls>();
            pad.lens = Object.FindFirstObjectByType<LensToolController>();
            pad.passthrough = Object.FindFirstObjectByType<PassthroughController>();
            pad.overlay = Object.FindFirstObjectByType<DebugOverlay>();
        }

        private static void AddIfMissing<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null) go.AddComponent<T>();
        }
    }
}
