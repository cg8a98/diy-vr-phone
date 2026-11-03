using UnityEngine;

namespace DiyVr.Stereo
{
    /// <summary>
    /// Two-eye camera rig for a phone in a VR viewer.
    /// Each eye camera renders into its own RenderTexture with an asymmetric frustum; a third (orthographic)
    /// camera draws two distortion meshes that show those textures on the left and right halves of the
    /// landscape screen. Mesh-based correction is what the Cardboard SDK does, and needs no URP renderer feature.
    /// Place this component on the "head" object; the head pose (1.5) will later drive its transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StereoRig : MonoBehaviour
    {
        public const int CompositeLayer = 31;

        [Tooltip("Lens profile. Leave empty to use the built-in defaults (Cardboard v1 lens, S23 Ultra screen).")]
        public LensProfile profile;
        [Tooltip("Shader DiyVr/StereoUnlit. Assigned in the scene so it is included in the build.")]
        public Shader unlitShader;
        [Range(8, 64)] public int meshGrid = 32;

        private Camera _left, _right, _composite;
        private RenderTexture _leftRt, _rightRt;
        private Material _leftMat, _rightMat;
        private Mesh _leftMesh, _rightMesh;
        private int _builtW, _builtH;
        private LensProfile _runtimeDefault;

        public Camera LeftEye { get { return _left; } }
        public Camera RightEye { get { return _right; } }

        private LensProfile Active
        {
            get
            {
                if (profile != null) return profile;
                if (_runtimeDefault == null) _runtimeDefault = LensProfile.CreateDefault();
                return _runtimeDefault;
            }
        }

        /// <summary>Changes eye spacing (metres) and rebuilds the rig.</summary>
        public void SetIpd(float metres)
        {
            Active.ipd = metres;
            Rebuild();
        }

        private void OnEnable()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Rebuild();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Update()
        {
            if (Screen.width != _builtW || Screen.height != _builtH) Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            Release();
            var lens = Active;
            var p = lens.ToParams();
            _builtW = Screen.width;
            _builtH = Screen.height;
            float longSide = Mathf.Max(_builtW, _builtH), shortSide = Mathf.Max(1, Mathf.Min(_builtW, _builtH));
            float aspect = longSide / shortSide;
            int eyeW = Mathf.Max(16, Mathf.RoundToInt(longSide * 0.5f * lens.renderScale));
            int eyeH = Mathf.Max(16, Mathf.RoundToInt(shortSide * lens.renderScale));

            var shader = unlitShader != null ? unlitShader : Shader.Find("DiyVr/StereoUnlit");
            var gl = StereoGeometry.ComputeEye(p, EyeSide.Left);
            var gr = StereoGeometry.ComputeEye(p, EyeSide.Right);

            _leftRt = NewRt(eyeW, eyeH, "LeftEyeRT");
            _rightRt = NewRt(eyeW, eyeH, "RightEyeRT");
            _left = NewEye("LeftEye", -p.Ipd / 2.0, gl, lens, _leftRt, -2);
            _right = NewEye("RightEye", p.Ipd / 2.0, gr, lens, _rightRt, -1);

            var compGo = new GameObject("Compositor");
            compGo.transform.SetParent(transform, false);
            compGo.transform.localPosition = new Vector3(0f, 0f, -5f);
            _composite = compGo.AddComponent<Camera>();
            _composite.orthographic = true;
            _composite.orthographicSize = 1f;
            _composite.aspect = aspect;
            _composite.nearClipPlane = 0.1f;
            _composite.farClipPlane = 20f;
            _composite.clearFlags = CameraClearFlags.SolidColor;
            _composite.backgroundColor = Color.black;
            _composite.cullingMask = 1 << CompositeLayer;
            _composite.depth = 0;
            _composite.allowMSAA = false;

            _leftMesh = StereoGeometry.BuildDistortionMesh(gl, aspect, meshGrid);
            _rightMesh = StereoGeometry.BuildDistortionMesh(gr, aspect, meshGrid);
            _leftMat = NewMat(shader, _leftRt);
            _rightMat = NewMat(shader, _rightRt);
            NewMeshObject("LeftDistortion", compGo.transform, _leftMesh, _leftMat);
            NewMeshObject("RightDistortion", compGo.transform, _rightMesh, _rightMat);
        }

        private static RenderTexture NewRt(int w, int h, string name)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.Default) { name = name };
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Bilinear;
            rt.Create();
            return rt;
        }

        private static Material NewMat(Shader shader, RenderTexture rt)
        {
            var m = new Material(shader);
            m.mainTexture = rt;
            m.SetFloat("_BlackOutside", 1f);
            return m;
        }

        private Camera NewEye(string name, double x, EyeGeometry g, LensProfile lens, RenderTexture rt, float depth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((float)x, 0f, 0f);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = ~(1 << CompositeLayer);
            cam.nearClipPlane = lens.nearClip;
            cam.farClipPlane = lens.farClip;
            cam.depth = depth;
            cam.targetTexture = rt;
            cam.allowMSAA = false;
            cam.projectionMatrix = StereoGeometry.Projection(g, lens.nearClip, lens.farClip);
            return cam;
        }

        private static void NewMeshObject(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name) { layer = CompositeLayer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, 5f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void Release()
        {
            DestroyChild(_left); DestroyChild(_right); DestroyChild(_composite);
            _left = _right = _composite = null;
            DestroyObj(_leftRt); DestroyObj(_rightRt);
            DestroyObj(_leftMat); DestroyObj(_rightMat);
            DestroyObj(_leftMesh); DestroyObj(_rightMesh);
            _leftRt = _rightRt = null; _leftMat = _rightMat = null; _leftMesh = _rightMesh = null;
        }

        private static void DestroyChild(Camera c)
        {
            if (c != null) DestroyObj(c.gameObject);
        }

        private static void DestroyObj(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }
    }
}
