using UnityEngine;

namespace DiyVr.Experiments.PlaceObjects
{
    /// <summary>Pure maths and rules for placing an object on a detected plane. No scene access, so EditMode tests cover it.</summary>
    public static class PlacementMath
    {
        /// <summary>Side length of the placed cube in metres (8 cm).</summary>
        public const float CubeSize = 0.08f;
        /// <summary>Most objects kept; placing another removes the oldest.</summary>
        public const int MaxObjects = 20;

        /// <summary>Centre of an object whose bottom face rests on the plane at <paramref name="hitPoint"/>.</summary>
        public static Vector3 ObjectCentre(Vector3 hitPoint, Vector3 planeNormal, float size)
        {
            return hitPoint + planeNormal.normalized * (size * 0.5f);
        }

        /// <summary>Upright on the plane (local up = normal), yawed so its local forward points at the viewer.</summary>
        public static Quaternion UprightFacingViewer(Vector3 planeNormal, Vector3 hitPoint, Vector3 viewerPosition)
        {
            Vector3 n = planeNormal.sqrMagnitude > 1e-8f ? planeNormal.normalized : Vector3.up;
            Vector3 toViewer = Vector3.ProjectOnPlane(viewerPosition - hitPoint, n);
            if (toViewer.sqrMagnitude < 1e-8f) toViewer = Vector3.ProjectOnPlane(Vector3.forward, n);
            if (toViewer.sqrMagnitude < 1e-8f) toViewer = Vector3.ProjectOnPlane(Vector3.right, n);
            return Quaternion.LookRotation(toViewer.normalized, n);
        }

        /// <summary>A pinch places only when it just started, a plane is under the gaze, and nothing else owns the pinch.</summary>
        public static bool ShouldPlace(bool pinchStarted, bool hasPlaneHit, bool fiveButtonTestOn, bool gazeOnGazeable)
        {
            return pinchStarted && hasPlaneHit && !fiveButtonTestOn && !gazeOnGazeable;
        }

        /// <summary>How many of the oldest objects to remove so that adding one more stays within <paramref name="max"/>.</summary>
        public static int OverflowToRemove(int currentCount, int max)
        {
            if (max < 1) max = 1;
            int over = currentCount + 1 - max;
            return over > 0 ? over : 0;
        }
    }
}
