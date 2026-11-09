using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace DiyVr.Tracking
{
    /// <summary>Builds the lightweight plane prefab at runtime (no asset): mesh + outline, unlit, semi-transparent.</summary>
    public static class PlanePrefabFactory
    {
        /// <summary>
        /// Returns a template object. It is active (so instances are active) but draws nothing: no mesh, no line points.
        /// The caller parents it somewhere harmless and destroys it with the rig.
        /// </summary>
        public static GameObject Create()
        {
            var go = new GameObject("Plane Template");
            go.AddComponent<ARPlane>();
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            // Sprites/Default is unlit, supports alpha and is always included in builds.
            var shader = Shader.Find("Sprites/Default");
            mr.sharedMaterial = new Material(shader) { color = new Color(0.2f, 0.9f, 0.3f, 0.25f) };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = new Material(shader);
            lr.widthMultiplier = 0.01f;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<ARPlaneMeshVisualizer>();
            go.AddComponent<PlaneVisual>();
            go.layer = 0; // default layer: eye culling excludes only 29/30/31
            return go;
        }
    }
}
