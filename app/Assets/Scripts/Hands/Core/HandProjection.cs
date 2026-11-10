using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// Camera intrinsics in pixels for the image the landmarks were found in (same convention as
    /// PassthroughLayout.IntrinsicsQuad: cx, cy measured from the top-left of the image, y down).
    /// </summary>
    public struct HandIntrinsics
    {
        public float Fx, Fy, Cx, Cy, Width, Height;

        public HandIntrinsics(float fx, float fy, float cx, float cy, float width, float height)
        {
            Fx = fx; Fy = fy; Cx = cx; Cy = cy; Width = width; Height = height;
        }

        public bool IsValid { get { return Fx > 0f && Fy > 0f && Width > 0f && Height > 0f; } }

        /// <summary>Guess used when the phone does not report intrinsics: centred principal point, 70 degree horizontal field of view.</summary>
        public static HandIntrinsics Fallback(float width, float height)
        {
            float f = width * 0.5f / Mathf.Tan(35f * Mathf.Deg2Rad);
            return new HandIntrinsics(f, f, width * 0.5f, height * 0.5f, width, height);
        }
    }

    /// <summary>The camera state captured with one hand-tracking frame.</summary>
    public struct HandFrameInfo
    {
        public HandIntrinsics Intrinsics;
        /// <summary>World pose of the AR camera when the frame was grabbed.</summary>
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
        /// <summary>When the camera exposed this frame, on the HandClock clock (acquire time when the camera gave no usable stamp).</summary>
        public double CaptureSeconds;
        /// <summary>Camera frame age when it was picked up, in ms; only meaningful when <see cref="AgeKnown"/>.</summary>
        public double AgeMs;
        public bool AgeKnown;
        /// <summary>Time the CPU image conversion took, in ms.</summary>
        public double ConvertMs;

        public Vector3 ToWorld(Vector3 cameraLocal) { return CameraPosition + CameraRotation * cameraLocal; }
    }

    /// <summary>
    /// Pure maths: MediaPipe's normalised 2D landmarks to 3D points in the AR camera's local space.
    ///
    /// Conventions (match PassthroughLayout): landmark x, y are 0..1 across the camera image, origin top-left, y down,
    /// no flip (the CPU image is passed to MediaPipe unrotated and unmirrored). Camera local space is Unity's:
    /// x right, y UP, z forward (out of the back camera). A pixel (u, v) looks along
    /// ((u - cx) / fx, (cy - v) / fy, 1). Landmark z is MediaPipe's relative depth: roughly the same scale as x
    /// (so z * Width pixels), wrist at about 0, negative = closer to the camera.
    /// </summary>
    public static class HandProjection
    {
        /// <summary>Real wrist to middle-finger knuckle distance of an adult hand, metres.</summary>
        public const float DefaultPalmLengthMetres = 0.085f;
        public const float MinDepthMetres = 0.15f;
        public const float MaxDepthMetres = 1.2f;
        public const int Wrist = 0;
        public const int MiddleMcp = 9;

        /// <summary>Unit-depth ray (z = 1) through pixel (u, v).</summary>
        public static Vector3 Direction(float u, float v, HandIntrinsics k)
        {
            return new Vector3((u - k.Cx) / k.Fx, (k.Cy - v) / k.Fy, 1f);
        }

        /// <summary>Pixel position of a normalised landmark.</summary>
        public static Vector2 ToPixel(Vector3 landmark, HandIntrinsics k)
        {
            return new Vector2(landmark.x * k.Width, landmark.y * k.Height);
        }

        /// <summary>Length in pixels of wrist to middle-finger knuckle, including the relative z so a tilted palm is not under-measured.</summary>
        public static float PalmPixelLength(Vector3[] lm, HandIntrinsics k)
        {
            if (lm == null || lm.Length <= MiddleMcp) return 0f;
            float dx = (lm[MiddleMcp].x - lm[Wrist].x) * k.Width;
            float dy = (lm[MiddleMcp].y - lm[Wrist].y) * k.Height;
            float dz = (lm[MiddleMcp].z - lm[Wrist].z) * k.Width;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>Pinhole: depth = f * realLength / pixelLength. Clamped to 0.15..1.2 m. Returns the far limit when the length is unusable.</summary>
        public static float DepthFromPalm(float focalPixels, float palmPixels, float palmMetres)
        {
            if (focalPixels <= 0f || palmPixels < 1e-3f || palmMetres <= 0f) return MaxDepthMetres;
            return Mathf.Clamp(focalPixels * palmMetres / palmPixels, MinDepthMetres, MaxDepthMetres);
        }

        /// <summary>Hand depth in metres from the palm size in the image.</summary>
        public static float EstimateDepth(Vector3[] lm, HandIntrinsics k, float palmMetres = DefaultPalmLengthMetres)
        {
            return DepthFromPalm(0.5f * (k.Fx + k.Fy), PalmPixelLength(lm, k), palmMetres);
        }

        /// <summary>
        /// All landmarks as points in camera local space. <paramref name="wristDepth"/> is the (smoothed) distance of the
        /// wrist along the camera's forward axis; each landmark then gets MediaPipe's relative z converted to metres.
        /// </summary>
        public static Vector3[] Project(Vector3[] lm, HandIntrinsics k, float wristDepth)
        {
            var result = new Vector3[lm.Length];
            float wristZ = lm.Length > 0 ? lm[Wrist].z : 0f;
            float metresPerZ = k.Width * wristDepth / (0.5f * (k.Fx + k.Fy)); // z * Width pixels, in metres at this depth
            for (int i = 0; i < lm.Length; i++)
            {
                Vector2 px = ToPixel(lm[i], k);
                float depth = Mathf.Max(0.05f, wristDepth + (lm[i].z - wristZ) * metresPerZ);
                result[i] = Direction(px.x, px.y, k) * depth;
            }
            return result;
        }
    }

    /// <summary>The 21 bones of a MediaPipe hand as landmark index pairs.</summary>
    public static class HandSkeleton
    {
        public static readonly int[,] Bones =
        {
            {0, 1}, {1, 2}, {2, 3}, {3, 4},
            {0, 5}, {5, 6}, {6, 7}, {7, 8},
            {5, 9}, {9, 10}, {10, 11}, {11, 12},
            {9, 13}, {13, 14}, {14, 15}, {15, 16},
            {13, 17}, {17, 18}, {18, 19}, {19, 20},
            {0, 17},
        };

        public static int BoneCount { get { return Bones.GetLength(0); } }
    }

    /// <summary>One Euro filter for a single value: little lag when moving fast, strong smoothing when still.</summary>
    public sealed class HandOneEuro
    {
        private readonly float _minCutoff, _beta, _dCutoff;
        private bool _has;
        private float _x, _dx, _t;

        public HandOneEuro(float minCutoff = 1.5f, float beta = 2f, float dCutoff = 1f)
        {
            _minCutoff = minCutoff; _beta = beta; _dCutoff = dCutoff;
        }

        public void Reset() { _has = false; }

        /// <summary>Feed a sample taken at time <paramref name="t"/> (seconds); returns the filtered value.</summary>
        public float Filter(float value, float t)
        {
            if (!_has || t <= _t)
            {
                _has = true; _x = value; _dx = 0f; _t = t;
                return value;
            }
            float dt = t - _t;
            _t = t;
            float rawD = (value - _x) / dt;
            _dx += Alpha(dt, _dCutoff) * (rawD - _dx);
            float cutoff = _minCutoff + _beta * Mathf.Abs(_dx);
            _x += Alpha(dt, cutoff) * (value - _x);
            return _x;
        }

        private static float Alpha(float dt, float cutoff)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f - Mathf.Exp(-dt / tau);
        }
    }

    /// <summary>Per-frame exponential chase toward the latest target; hides the 15 fps stepping of the landmarks.</summary>
    public static class HandChase
    {
        public static Vector3 Step(Vector3 current, Vector3 target, float dt, float timeConstant)
        {
            if (timeConstant <= 0f) return target;
            float a = 1f - Mathf.Exp(-Mathf.Max(0f, dt) / timeConstant);
            return Vector3.Lerp(current, target, a);
        }
    }
}
