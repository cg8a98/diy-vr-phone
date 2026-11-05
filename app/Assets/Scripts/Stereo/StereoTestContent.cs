using UnityEngine;

namespace DiyVr.Stereo
{
    /// <summary>
    /// Builds the stereo test content at start: a procedural line grid (to judge distortion; straight lines
    /// must look straight at the edges) and a 0.2 m cube 1 m in front of the starting pose.
    /// The grid is head-locked; the cube is fixed in the world.
    /// </summary>
    public sealed class StereoTestContent : MonoBehaviour
    {
        public Shader unlitShader;
        [Tooltip("Rig whose forward direction places the content. Defaults to the first StereoRig found.")]
        public Transform head;
        public float gridDistance = 2.5f;
        public float gridHeight = 3f;     // metres, grid is gridHeight tall and 2x as wide
        public float cubeDistance = 1f;
        public float cubeSize = 0.2f;

        private GameObject _grid;
        private bool _gridVisible = true;

        private void Start()
        {
            if (head == null)
            {
                var rig = FindFirstObjectByType<StereoRig>();
                head = rig != null ? rig.transform : transform;
            }
            var shader = unlitShader != null ? unlitShader : Shader.Find("DiyVr/StereoUnlit");
            BuildGrid(shader);
            BuildCube(shader);
        }

        /// <summary>
        /// Shows or hides the distortion grid (hidden while the camera view is on, so it does not cover the camera
        /// image). The cube is never touched. Safe to call before the grid is built; it applies once built.
        /// </summary>
        public void SetGridVisible(bool visible)
        {
            _gridVisible = visible;
            if (_grid != null) _grid.SetActive(visible);
        }

        private void BuildGrid(Shader shader)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DistortionGrid";
            DestroyCollider(quad);
            quad.transform.SetParent(head, false);
            quad.transform.localPosition = new Vector3(0f, 0f, gridDistance);
            quad.transform.localScale = new Vector3(gridHeight * 2f, gridHeight, 1f);
            var mat = new Material(shader) { mainTexture = MakeGridTexture(1024, 512, 16) };
            quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
            _grid = quad;
            _grid.SetActive(_gridVisible);
        }

        private void BuildCube(Shader shader)
        {
            CreateCube(shader, head, cubeDistance, cubeSize);
        }

        /// <summary>
        /// World-space spot for the cube: <paramref name="distance"/> metres ahead of the head, along its forward
        /// direction flattened onto the horizontal plane (so it is not up in the ceiling if the phone starts tilted).
        /// </summary>
        public static Vector3 CubeWorldPosition(Vector3 headPosition, Quaternion headRotation, float distance)
        {
            Vector3 fwd = headRotation * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward; // looking straight up or down
            return headPosition + fwd.normalized * distance;
        }

        /// <summary>
        /// Creates the test cube fixed in the world (NOT parented to the head, so it stays still while the phone moves).
        /// The AR origin starts at the device pose, so the rig's pose at start is the room-anchored starting pose.
        /// </summary>
        public static GameObject CreateCube(Shader shader, Transform head, float distance, float size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestCube";
            DestroyCollider(cube);
            Vector3 pos = head != null ? head.position : Vector3.zero;
            Quaternion rot = head != null ? head.rotation : Quaternion.identity;
            cube.transform.SetParent(null, false);
            cube.transform.SetPositionAndRotation(CubeWorldPosition(pos, rot, distance),
                Quaternion.Euler(0f, rot.eulerAngles.y, 0f));
            cube.transform.localScale = Vector3.one * size;
            if (shader != null)
            {
                var mat = new Material(shader) { mainTexture = MakeCheckerTexture(64, 8), color = new Color(1f, 0.6f, 0.2f) };
                cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            return cube;
        }

        private static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c == null) return;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }

        /// <summary>White lines on dark blue every <c>cells</c> pixels' worth; centre lines red; border green.</summary>
        public static Texture2D MakeGridTexture(int w, int h, int cellsAcrossHeight)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var px = new Color32[w * h];
            float cell = h / (float)cellsAcrossHeight;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x < 3 || y < 3 || x >= w - 3 || y >= h - 3;
                    bool centre = Mathf.Abs(x - w / 2) < 2 || Mathf.Abs(y - h / 2) < 2;
                    bool line = Mathf.Repeat(x, cell) < 1.5f || Mathf.Repeat(y, cell) < 1.5f;
                    Color32 c = new Color32(10, 10, 40, 255);
                    if (line) c = new Color32(230, 230, 230, 255);
                    if (centre) c = new Color32(255, 40, 40, 255);
                    if (border) c = new Color32(40, 255, 40, 255);
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        public static Texture2D MakeCheckerTexture(int size, int squares)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            int sq = size / squares;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = ((x / sq + y / sq) % 2 == 0) ? new Color32(255, 255, 255, 255) : new Color32(90, 90, 90, 255);
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }
    }
}
