using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using MpImage = Mediapipe.Image;
using MpImageFormat = Mediapipe.ImageFormat.Types.Format;
using MpRunningMode = Mediapipe.Tasks.Vision.Core.RunningMode;

namespace DiyVr.Hands
{
    /// <summary>
    /// Runs the MediaPipe HandLandmarker on ARCore's camera image (milestone 2.1 spike).
    ///
    /// Main thread each frame: if no inference is running, grab the latest CPU image, convert it to RGBA32 at a
    /// reduced size straight into a reusable buffer, dispose the XRCpuImage, and wake the worker. The worker thread
    /// runs HandLandmarker in VIDEO mode (CPU delegate), so the main thread never waits on inference. At most one
    /// inference is in flight; frames that arrive meanwhile are simply not acquired.
    ///
    /// If anything fails to start (model missing, native library missing, no camera) the service writes the reason to
    /// <see cref="Status"/> and <see cref="HandsStatus"/>, then disables itself. Camera and hand data stay in memory
    /// on the phone; nothing is saved or sent.
    /// </summary>
    public sealed class HandTrackingService : MonoBehaviour
    {
        public const string ModelFileName = "hand_landmarker.task";

        [SerializeField] private int maxHands = 2;
        [SerializeField] private int maxImageWidth = HandImageSize.DefaultMaxWidth;

        private static readonly IReadOnlyList<HandPose> NoHands = new HandPose[0];

        private readonly RollingAverage _inferenceMs = new RollingAverage(20);
        private readonly RateMeter _rate = new RateMeter(16);
        private readonly object _lock = new object();
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

        private HandLandmarker _landmarker;
        private ARCameraManager _camera;
        private Thread _worker;
        private SemaphoreSlim _wake;
        private volatile bool _quit;
        private int _busy; // 1 while the worker owns the buffer; touched with Interlocked
        private long _lastTimestampMs = -1;

        private NativeArray<byte> _buffer;
        private int _bufW, _bufH;
        private bool _ready;

        // Written by the worker under _lock, read by the main thread.
        private IReadOnlyList<HandPose> _pendingHands = NoHands;
        private double _pendingMs;
        private bool _hasPending;
        private string _workerError;

        /// <summary>Why the service is off, or "running" / "starting".</summary>
        public string Status { get; private set; } = "starting";

        public int HandCount { get; private set; }
        public double LandmarkFps { get; private set; }
        public double InferenceMs { get; private set; }

        /// <summary>Latest result, newest first replaced each time a frame finishes. Never null.</summary>
        public IReadOnlyList<HandPose> LatestHands { get; private set; } = NoHands;

        private void OnEnable()
        {
            HandsStatus.SetOff("starting");
            StartCoroutine(Init());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Shutdown();
            if (Status == "running" || Status == "starting") Status = "stopped";
            HandsStatus.SetOff(Status);
        }

        private void Fail(string reason)
        {
            Status = reason;
            Debug.LogWarning("Hand tracking off: " + reason);
            HandsStatus.SetOff(reason);
            enabled = false; // OnDisable cleans up and keeps this reason
        }

        private IEnumerator Init()
        {
            // 1. Model bytes from StreamingAssets (inside the APK on Android, so UnityWebRequest reads it).
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, ModelFileName);
            if (!path.Contains("://")) path = "file://" + path;
            byte[] model = null;
            using (var req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) model = req.downloadHandler.data;
            }
            if (model == null || model.Length == 0)
            {
                Fail("model missing");
                yield break;
            }

            // 2. The landmarker. DllNotFoundException here means the native libraries are not in the build.
            try
            {
                var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU, modelAssetBuffer: model);
                var options = new HandLandmarkerOptions(baseOptions, MpRunningMode.VIDEO, numHands: Mathf.Max(1, maxHands));
                _landmarker = HandLandmarker.CreateFromOptions(options);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Fail("init failed: " + e.GetType().Name);
                yield break;
            }

            // 3. The camera.
            for (float waited = 0f; _camera == null && waited < 10f; waited += Time.unscaledDeltaTime)
            {
                _camera = FindFirstObjectByType<ARCameraManager>();
                if (_camera == null) yield return null;
            }
            if (_camera == null)
            {
                Fail("no AR camera");
                yield break;
            }

            _quit = false;
            _wake = new SemaphoreSlim(0);
            _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "HandLandmarker" };
            _worker.Start();

            Status = "running";
            _ready = true;
            HandsStatus.Active = true;
            HandsStatus.Reason = string.Empty;
        }

        private void Update()
        {
            if (!_ready) return;

            PublishResults();
            if (Volatile.Read(ref _busy) == 0) TryStartFrame();
        }

        private void TryStartFrame()
        {
            if (_camera == null || !_camera.TryAcquireLatestCpuImage(out XRCpuImage image))
                return;

            try
            {
                HandImageSize.Choose(image.width, image.height, maxImageWidth, out int w, out int h);
                if (w == 0) return;

                if (!_buffer.IsCreated || w != _bufW || h != _bufH)
                {
                    if (_buffer.IsCreated) _buffer.Dispose();
                    _buffer = new NativeArray<byte>(w * h * 4, Allocator.Persistent);
                    _bufW = w;
                    _bufH = h;
                }

                var conversion = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(w, h),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.None,
                };
                image.Convert(conversion, _buffer);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Fail("camera image convert failed");
                return;
            }
            finally
            {
                image.Dispose(); // every acquired image, every frame
            }

            Volatile.Write(ref _busy, 1);
            _wake.Release();
        }

        // Worker thread: one frame per wake-up.
        private void WorkerLoop()
        {
            var result = HandLandmarkerResult.Alloc(Mathf.Max(1, maxHands));
            while (true)
            {
                _wake.Wait();
                if (_quit) return;

                try
                {
                    long ts = _clock.ElapsedMilliseconds;
                    if (ts <= _lastTimestampMs) ts = _lastTimestampMs + 1; // MediaPipe needs strictly increasing times
                    _lastTimestampMs = ts;

                    long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                    IReadOnlyList<HandPose> hands = NoHands;
                    using (var image = new MpImage(MpImageFormat.Srgba, _bufW, _bufH, _bufW * 4, _buffer))
                    {
                        if (_landmarker.TryDetectForVideo(image, ts, null, ref result))
                            hands = ToPoses(result);
                    }
                    double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

                    lock (_lock)
                    {
                        _pendingHands = hands;
                        _pendingMs = ms;
                        _hasPending = true;
                    }
                }
                catch (Exception e)
                {
                    lock (_lock) _workerError = e.GetType().Name + ": " + e.Message;
                }
                finally
                {
                    Volatile.Write(ref _busy, 0);
                }
            }
        }

        private static IReadOnlyList<HandPose> ToPoses(HandLandmarkerResult r)
        {
            if (r.handLandmarks == null || r.handLandmarks.Count == 0) return NoHands;

            var poses = new List<HandPose>(r.handLandmarks.Count);
            for (int i = 0; i < r.handLandmarks.Count; i++)
            {
                List<NormalizedLandmark> src = r.handLandmarks[i].landmarks;
                if (src == null || src.Count < HandPose.LandmarkCount) continue;

                var pts = new Vector3[HandPose.LandmarkCount];
                for (int k = 0; k < pts.Length; k++) pts[k] = new Vector3(src[k].x, src[k].y, src[k].z);

                HandSide side = HandSide.Unknown;
                float score = 0f;
                if (r.handedness != null && i < r.handedness.Count && r.handedness[i].categories != null
                    && r.handedness[i].categories.Count > 0)
                {
                    Category c = r.handedness[i].categories[0];
                    side = HandPose.ParseSide(c.categoryName);
                    score = c.score;
                }
                poses.Add(new HandPose(pts, side, score));
            }
            return poses;
        }

        // Main thread: move the worker's newest result into the public properties.
        private void PublishResults()
        {
            IReadOnlyList<HandPose> hands = null;
            double ms = 0;
            string error = null;
            lock (_lock)
            {
                if (_hasPending)
                {
                    hands = _pendingHands;
                    ms = _pendingMs;
                    _hasPending = false;
                }
                error = _workerError;
                _workerError = null;
            }

            if (error != null)
            {
                Fail("inference failed: " + error);
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            if (hands != null)
            {
                LatestHands = hands;
                HandCount = hands.Count;
                _inferenceMs.Add(ms);
                _rate.Tick(now);
            }
            InferenceMs = _inferenceMs.Average;
            LandmarkFps = _rate.Rate(now);

            HandsStatus.Active = true;
            HandsStatus.HandCount = HandCount;
            HandsStatus.LandmarkFps = LandmarkFps;
            HandsStatus.InferenceMs = InferenceMs;
        }

        private void Shutdown()
        {
            _ready = false;
            if (_worker != null)
            {
                _quit = true;
                _wake.Release();
                _worker.Join(3000); // an inference is at most a few hundred ms
                _worker = null;
            }
            if (_landmarker != null)
            {
                try { ((IDisposable)_landmarker).Dispose(); } // Dispose is an explicit interface method (it also calls Close) catch (Exception e) { Debug.LogException(e); }
                _landmarker = null;
            }
            if (_buffer.IsCreated) _buffer.Dispose();
            if (_wake != null) { _wake.Dispose(); _wake = null; }
            Volatile.Write(ref _busy, 0);
            LatestHands = NoHands;
            HandCount = 0;
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
