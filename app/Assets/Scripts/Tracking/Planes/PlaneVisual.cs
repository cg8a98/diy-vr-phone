using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace DiyVr.Tracking
{
    /// <summary>
    /// Per-plane visual: shows the mesh and outline only while <see cref="PlaneDebugView.Visible"/>, coloured by
    /// <see cref="PlaneClassifier"/>. Lives on the runtime plane prefab built by <see cref="PlanePrefabFactory"/>.
    /// Default layer, so both eye cameras draw it.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    public sealed class PlaneVisual : MonoBehaviour
    {
        private MeshRenderer _mesh;
        private LineRenderer _line;
        private PlaneKind _kind = (PlaneKind)(-1);

        private void Awake()
        {
            _mesh = GetComponent<MeshRenderer>();
            _line = GetComponent<LineRenderer>();
        }

        // LateUpdate so this wins over ARPlaneMeshVisualizer toggling the renderer.
        private void LateUpdate()
        {
            bool show = PlaneDebugView.Visible;
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
