using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Tracking
{
    /// <summary>
    /// Shows or hides the detected-plane visuals (default hidden). Planes are only drawn; they never feed the head pose.
    /// <see cref="Visible"/> and <see cref="PlaneCount"/> are static so later code (overlay, placement) can read them.
    /// Added to the AR rig by <see cref="HeadPoseDriver"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaneDebugView : MonoBehaviour
    {
        /// <summary>True while plane visuals are shown. Default false.</summary>
        public static bool Visible { get; private set; }
        /// <summary>Number of live planes: tracking and not merged into another plane.</summary>
        public static int PlaneCount { get; private set; }
        /// <summary>Height (world Y) of the lowest tracked plane; 0 when none.</summary>
        public static float LowestHeight { get; private set; }

        public ARPlaneManager planeManager;

        private readonly List<float> _heights = new List<float>();

        public void Toggle() { Visible = !Visible; }
        public void SetVisible(bool visible) { Visible = visible; }

        private void OnDisable() { Visible = false; PlaneCount = 0; LowestHeight = 0f; }

        private void Update()
        {
            _heights.Clear();
            if (planeManager != null)
            {
                foreach (ARPlane p in planeManager.trackables)
                    if (p != null && PlaneClassifier.IsLivePlane(p.subsumedBy != null,
                        p.trackingState == TrackingState.Tracking, p.trackableId != TrackableId.invalidId))
                        _heights.Add(p.transform.position.y);
            }
            PlaneCount = _heights.Count;
            LowestHeight = PlaneClassifier.LowestHeight(_heights);
        }
    }
}
