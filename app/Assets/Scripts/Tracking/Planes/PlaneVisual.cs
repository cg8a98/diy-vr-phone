using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Tracking
{
    /// <summary>
    /// Per-plane visual: shows the mesh and outline only while <see cref="PlaneDebugView.Visible"/>, coloured by
    /// <see cref="PlaneClassifier"/>. Lives on the runtime plane prefab built by <see cref="PlanePrefabFactory"/>.
    /// Default layer, so both eye cameras draw it.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    // Runs after ARPlaneMeshVisualizer (default order 0) in every update phase, so our on/off decision is the last word.
    [DefaultExecutionOrder(1000)]
    public sealed class PlaneVisual : MonoBehaviour
    {
        private ARPlane _plane;
        private MeshRenderer _mesh;
        private LineRenderer _line;
        private PlaneKind _kind = (PlaneKind)(-1);

        private void Awake()
        {
            _plane = GetComponent<ARPlane>();
            _mesh = GetComponent<MeshRenderer>();
            _line = GetComponent<LineRenderer>();
        }

        // LateUpdate + late execution order so this wins over ARPlaneMeshVisualizer toggling the renderer.
        // Merged (subsumed) and non-tracking planes stay hidden, as ARPlaneMeshVisualizer would do on its own.
        // The prefab template has no trackable id, so it can never draw.
        private void LateUpdate()
        {
            bool show = PlaneClassifier.ShouldShow(PlaneDebugView.Visible, _plane.subsumedBy != null,
                _plane.trackingState == TrackingState.Tracking, _plane.trackableId != TrackableId.invalidId);
            if (_mesh != null) _mesh.enabled = show;
            if (_line != null) _line.enabled = show;
            if (!show) return;
            PlaneKind kind = PlaneClassifier.Classify(transform.position.y, PlaneDebugView.LowestHeight);
            if (kind == _kind) return;
            _kind = kind;
            Color c = kind == PlaneKind.Floor ? new Color(0.2f, 0.9f, 0.3f)
                    : kind == PlaneKind.Desk ? new Color(0.2f, 0.6f, 1f)
                    : new Color(1f, 0.8f, 0.2f);
            if (_mesh != null) _mesh.material.color = new Color(c.r, c.g, c.b, 0.25f);
            if (_line != null) { _line.startColor = c; _line.endColor = c; }
        }
    }
}
