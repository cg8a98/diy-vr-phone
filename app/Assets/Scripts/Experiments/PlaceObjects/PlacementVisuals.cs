using UnityEngine;

namespace DiyVr.Experiments.PlaceObjects
{
    /// <summary>Builds the placement preview disc and the placed cube. Static so an EditMode test can run the real code.</summary>
    public static class PlacementVisuals
    {
        public const float PreviewDiameter = 0.12f;
        public const float PreviewThickness = 0.002f;
        private static readonly Color PreviewColor = new Color(1f, 0.9f, 0.2f);
        private static readonly Color CubeColor = new Color(1f, 0.45f, 0.1f);

        /// <summary>A flat disc whose local up is its axis. The caller places and tilts it.</summary>
        public static GameObject CreatePreview()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "PlacePreview";
            Prepare(go, PreviewColor);
            // Unity's cylinder is 2 units tall and 1 wide at scale 1, so y scale is half the thickness.
            go.transform.localScale = new Vector3(PreviewDiameter, PreviewThickness * 0.5f, PreviewDiameter);
            return go;
        }

        /// <summary>An unlit 8 cm cube with its pivot at the centre.</summary>
        public static GameObject CreateCube()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "PlacedCube";
            Prepare(go, CubeColor);
            go.transform.localScale = Vector3.one * PlacementMath.CubeSize;
            return go;
        }

        private static void Prepare(GameObject go, Color colour)
        {
            go.layer = 0; // the eye cameras only skip layers 29-31
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyObject(col); // never in the way of the gaze ray
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(UnlitShader()) { name = "PlaceMat", color = colour };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // Destroy is an error in EditMode (tests) and DestroyImmediate is wrong in play mode.
        private static void DestroyObject(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        private static Shader UnlitShader()
        {
            Shader s = Shader.Find("DiyVr/StereoUnlit");
            if (s == null) s = Shader.Find("Sprites/Default");
            return s;
        }
    }
}
