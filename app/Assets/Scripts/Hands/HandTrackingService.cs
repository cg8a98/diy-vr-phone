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
    /// runs HandLandmarker in VIDEO mode (GPU delegate, CPU as fallback), so the main thread never waits on inference. At most one
    /// inference is in flight; frames that arrive meanwhile are simply not acquired, so the next one is always the newest
    /// camera frame (stale frames are dropped, never queued), and a camera frame is never processed twice.
    ///
    /// If anything fails to start (model missing, native library missing, no camera) the service writes the reason to
    /// <see cref="Status"/> and <see cref="HandsStatus"/>, then disables itself. Camera and hand data stay in memory
    /// on the phone; nothing is saved or sent.
    /// </summary>
    [DefaultExecutionOrder(-100)] // publish results before HandViewManager (-50) and the views draw, so no frame is lost
    public sealed class HandTrackingService : MonoBehaviour
    {
        public const string ModelFileName = "hand_landmarker.task";

        [SerializeField] private int maxHands = 2;
        [SerializeField] private int maxImageWidth = HandImageSize.DefaultMaxWidth;

        // Lower than MediaPipe's default 0.5 so hands survive dim light and motion blur. Cost: more false hands on
        // clutter and a little jitter on weak detections.
        public const float MinDetectionConfidence = 0.35f;
        public const float MinPresenceConfidence = 0.35f;
        public const float MinTrackingConfidence = 0.35f;
        public const float StartDelaySeconds = 3f;

        private static readonly IReadOnlyList<HandPose> NoHands = new HandPose[0];
        private static readonly IReadOnlyList<int> NoSlots = new int[0];
        private readonly HandIdentity _identity = new HandIdentity();

        private readonly RollingAverage _inferenceMs = new RollingAverage(20);
        private readonly RollingAverage _ageMs = new RollingAverage(20);
        private readonly RollingAverage _convertMs = new RollingAverage(20);
        private readonly RateMeter _rate = new RateMeter(16);
        private readonly CameraAgeProbe _camProbe = new CameraAgeProbe(); // 2.2e diagnostics: what the camera age is made of
        private bool _subscribed, _modesRead, _busyAtUpdate;
        private readonly object _lock = new object();
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

        private HandLandmarker _landmarker;
        private ARCameraManager _camera;
        private Thread _worker;
        private SemaphoreSlim _wake;
        private volatile bool _quit;
        private int _busy; // 1 while the worker owns the buffer; touched with Interlocked
        private long _lastTimestampMs = -1;
        private double _lastImageStamp = -1; // camera stamp of the last frame handed to the worker
        private string _delegate = string.Empty;
        private bool _forceCpu;

        private NativeArray<byte> _buffer;
        private int _bufW, _bufH;
        private bool _ready;

        // Written by the worker under _lock, read by the main thread.
        private IReadOnlyList<HandPose> _pendingHands = NoHands;
        private double _pendingMs;
        private bool _hasPending;
        private string _workerError;

        // Camera state of the frame being processed: written on the main thread before the worker is woken, copied to
        // _pendingFrame by the worker with the result (at most one frame is in flight, so it cannot change meanwhile).
        private HandFrameInfo _inFlightFrame;
        private HandFrameInfo _pendingFrame;

        /// <summary>Why the service is off, or "running" / "starting".</summary>
        public string Status { get; private set; } = "starting";

        /// <summary>"GPU" or "CPU": what the landmarker runs on.</summary>
        public string Delegate { get { return _delegate; } }

        public int HandCount { get; private set; }
        public double LandmarkFps { get; private set; }
        public double InferenceMs { get; private set; }

        /// <summary>Latest result, newest first replaced each time a frame finishes. Never null.</summary>
        public IReadOnlyList<HandPose> LatestHands { get; private set; } = NoHands;

        /// <summary>Intrinsics and AR camera world pose captured when the frame behind <see cref="LatestHands"/> was grabbed.</summary>
        public HandFrameInfo LatestFrame { get; private set; }

        /// <summary>
        /// Stable slot (0 or 1) for each hand in <see cref="LatestHands"/>, same order; -1 = no slot. A hand keeps its
        /// slot while it stays in view even when MediaPipe's Left/Right label flips. Never null.
        /// </summary>
        public IReadOnlyList<int> LatestSlots { get; private set; } = NoSlots;

        /// <summary>Increases by one every time a new result (even "no hands") is published.</summary>
        public int ResultVersion { get; private set; }

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
            // 0. Let the update check (UpdateChecker sends it at launch) go out before anything risky runs.
            yield return new WaitForSecondsRealtime(StartDelaySeconds);

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

            // 2. The landmarker. GPU first (much faster inference), CPU if creating it throws or a GPU run fails later.
            //    DllNotFoundException here means the native libraries are not in the build.
            string delegateName = null;
            Exception lastError = null;
            // Crash guard: a native GPU crash cannot be caught. Flag the try first; clear it after the first GPU result.
            bool wasBlocked = PlayerPrefs.GetInt(HandGpuGuard.BlockedKey, 0) == 1;
            bool nowBlocked;
            bool tryGpu = HandGpuGuard.ShouldTryGpu(PlayerPrefs.GetInt(HandGpuGuard.PendingKey, 0) == 1, wasBlocked, out nowBlocked)
                && !_forceCpu;
            if (nowBlocked && !wasBlocked) { PlayerPrefs.SetInt(HandGpuGuard.BlockedKey, 1); PlayerPrefs.Save(); }
            if (tryGpu) { PlayerPrefs.SetInt(HandGpuGuard.PendingKey, 1); PlayerPrefs.Save(); }
            var order = !tryGpu
                ? new[] { Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU }
                : new[] { Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU, Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU };
            foreach (var d in order)
            {
                try
                {
                    var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(d, modelAssetBuffer: model);
                    var options = new HandLandmarkerOptions(baseOptions, MpRunningMode.VIDEO, numHands: Mathf.Max(1, maxHands),
                        minHandDetectionConfidence: MinDetectionConfidence,
                        minHandPresenceConfidence: MinPresenceConfidence,
                        minTrackingConfidence: MinTrackingConfidence);
                    _landmarker = HandLandmarker.CreateFromOptions(options);
                    delegateName = d == Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU ? "GPU" : "CPU";
                    break;
                }
                catch (Exception e)
                {
                    lastError = e;
                    Debug.LogWarning("Hand landmarker " + d + " delegate failed: " + e.GetType().Name + ": " + e.Message);
                }
            }
            if (_landmarker == null)
            {
                if (lastError != null) Debug.LogException(lastError);
                Fail("init failed: " + (lastError != null ? lastError.GetType().Name : "unknown"));
                yield break;
            }
            _delegate = delegateName == "CPU" && nowBlocked ? "CPU (GPU blocked)" : delegateName;
            if (tryGpu && delegateName != "GPU") ClearGpuPending(false); // creation threw: no crash, just no GPU

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

            _camProbe.Reset();
            _modesRead = false;
            _camera.frameReceived += OnCameraFrame;
            _subscribed = true;

            _quit = false;
            _wake = new SemaphoreSlim(0);
            _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "HandLandmarker" };
            _worker.Start();

            Status = "running";
            _ready = true;
            HandsStatus.Active = true;
            HandsStatus.Reason = string.Empty;
            HandsStatus.Delegate = _delegate;
        }

        private void Update()
        {
            if (!_ready) return;

            PublishResults();
            if (!_ready) return; // PublishResults can stop or restart the service
            _busyAtUpdate = Volatile.Read(ref _busy) != 0;
            if (!_busyAtUpdate) TryStartFrame();
        }

        // Second chance in the same frame: if the worker finished between Update and now, hand it the next camera
        // frame straight away instead of leaving it idle until the next Update (up to one screen refresh later).
        private void LateUpdate()
        {
            if (!_ready || !_busyAtUpdate) return;
            _busyAtUpdate = false;
            if (Volatile.Read(ref _busy) == 0) TryStartFrame();
        }

        // Diagnostics only (2.2e). Raised by ARCameraManager on the main thread, every app frame; the stamp only
        // changes when the camera has delivered a new picture.
        private void OnCameraFrame(ARCameraFrameEventArgs e)
        {
            if (!_ready) return;
            if (!_modesRead) ReadCameraModes();
            double stamp = e.timestampNs.HasValue ? e.timestampNs.Value * 1e-9 : 0.0;
            double exposure = e.exposureDuration.HasValue ? e.exposureDuration.Value : -1.0;
            _camProbe.OnCameraFrame(stamp, HandClock.Now(), exposure);
        }

        // Highest frame rate among ARCore's camera modes on this phone, for the overlay (the mode itself is chosen
        // elsewhere, in HeadPoseDriver).
        private void ReadCameraModes()
        {
            _modesRead = true;
            NativeArray<XRCameraConfiguration> configs = default(NativeArray<XRCameraConfiguration>);
            try
            {
                configs = _camera.GetConfigurations(Allocator.Temp);
                int max = 0;
                if (configs.IsCreated)
                {
                    for (int i = 0; i < configs.Length; i++)
                    {
                        object fps = configs[i].framerate; // int? in AR Foundation; boxing keeps this compile-safe
                        if (fps is int && (int)fps > max) max = (int)fps;
                    }
                }
                _camProbe.SetMaxModeFps(max);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Camera mode list failed: " + e.Message);
            }
            finally
            {
                if (configs.IsCreated) configs.Dispose();
            }
        }

        private void TryStartFrame()
        {
            if (_camera == null || !_camera.TryAcquireLatestCpuImage(out XRCpuImage image))
                return;

            try
            {
                // Same camera image as last time: the next one has not arrived yet. Wait for it instead of
                // spending an inference on a duplicate (which would also delay the real next frame).
                double stamp = image.timestamp;
                if (!HandLatency.IsNewFrame(_lastImageStamp, stamp)) return;
                double acquiredAt = HandClock.Now();
                _camProbe.OnAcquire(stamp, acquiredAt, HandClock.NowMonotonic());

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
                long c0 = System.Diagnostics.Stopwatch.GetTimestamp();
                image.Convert(conversion, _buffer);
                double convertMs = (System.Diagnostics.Stopwatch.GetTimestamp() - c0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                _lastImageStamp = stamp;
                CaptureFrameInfo(w, h);

                double age = HandLatency.AgeSeconds(stamp, acquiredAt);
                _inFlightFrame.AgeKnown = age >= 0.0;
                _inFlightFrame.AgeMs = age * 1000.0;
                _inFlightFrame.CaptureSeconds = age >= 0.0 ? stamp : acquiredAt;
                _inFlightFrame.ConvertMs = convertMs;
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

        // Main thread. Pose = the AR camera's transform right now (the latest pose, not an interpolation to the image
        // timestamp). Intrinsics are for the full camera image; they are rescaled to the (uniformly downscaled) size
        // the landmarks are normalised against, so pixels and intrinsics agree.
        private void CaptureFrameInfo(int w, int h)
        {
            HandIntrinsics k;
            XRCameraIntrinsics ci;
            if (_camera.TryGetIntrinsics(out ci) && ci.resolution.x > 0 && ci.resolution.y > 0
                && ci.focalLength.x > 0f && ci.focalLength.y > 0f)
            {
                float sx = (float)w / ci.resolution.x, sy = (float)h / ci.resolution.y;
                k = new HandIntrinsics(ci.focalLength.x * sx, ci.focalLength.y * sy,
                    ci.principalPoint.x * sx, ci.principalPoint.y * sy, w, h);
            }
            else
            {
                k = HandIntrinsics.Fallback(w, h);
            }
            Transform t = _camera.transform;
            _inFlightFrame = new HandFrameInfo { Intrinsics = k, CameraPosition = t.position, CameraRotation = t.rotation };
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
                            hands = HandDedupe.Suppress(ToPoses(result), _bufH > 0 ? (float)_bufW / _bufH : 1f);
                    }
                    double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

                    lock (_lock)
                    {
                        _pendingHands = hands;
                        _pendingMs = ms;
                        _pendingFrame = _inFlightFrame;
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
            HandFrameInfo frame = default(HandFrameInfo);
            string error = null;
            lock (_lock)
            {
                if (_hasPending)
                {
                    hands = _pendingHands;
                    ms = _pendingMs;
                    frame = _pendingFrame;
                    _hasPending = false;
                }
                error = _workerError;
                _workerError = null;
            }

            if (error != null && _delegate == "GPU")
            {
                // The GPU delegate failed while running: rebuild on the CPU delegate instead of giving up.
                Debug.LogWarning("Hand tracking GPU run failed, switching to CPU: " + error);
                _forceCpu = true;
                ClearGpuPending(false);
                Shutdown();
                Status = "starting";
                HandsStatus.SetOff("switching to CPU");
                StartCoroutine(Init());
                return;
            }
            if (error != null)
            {
                Fail("inference failed: " + error);
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            if (hands != null)
            {
                if (_delegate == "GPU" && PlayerPrefs.GetInt(HandGpuGuard.PendingKey, 0) == 1) ClearGpuPending(true);
                LatestHands = hands;
                LatestFrame = frame;
                float aspect = frame.Intrinsics.IsValid ? frame.Intrinsics.Width / frame.Intrinsics.Height : 1f;
                LatestSlots = _identity.Assign(hands, aspect, now);
                ResultVersion++;
                HandCount = hands.Count;
                _inferenceMs.Add(ms);
                _convertMs.Add(frame.ConvertMs);
                if (frame.AgeKnown) _ageMs.Add(frame.AgeMs);
                _rate.Tick(now);
            }
            InferenceMs = _inferenceMs.Average;
            LandmarkFps = _rate.Rate(now);

            HandsStatus.Active = true;
            HandsStatus.HandCount = HandCount;
            HandsStatus.LandmarkFps = LandmarkFps;
            HandsStatus.InferenceMs = InferenceMs;
            HandsStatus.Delegate = _delegate;
            HandsStatus.LagConvertMs = _convertMs.Average;
            HandsStatus.LagAgeMs = _ageMs.Count > 0 ? _ageMs.Average : -1;
            if (hands != null) HandsStatus.CameraAgeLines = _camProbe.Lines(); // rebuilt per result, not per frame
        }

        private static void ClearGpuPending(bool ok)
        {
            PlayerPrefs.SetInt(HandGpuGuard.PendingKey, 0);
            if (ok) PlayerPrefs.SetInt(HandGpuGuard.OkKey, 1);
            PlayerPrefs.Save();
        }

        private void Shutdown()
        {
            _ready = false;
            if (_subscribed)
            {
                if (_camera != null) _camera.frameReceived -= OnCameraFrame;
                _subscribed = false;
            }
            if (_worker != null)
            {
                _quit = true;
                _wake.Release();
                _worker.Join(3000); // an inference is at most a few hundred ms
                _worker = null;
            }
            if (_landmarker != null)
            {
                // Dispose is an explicit interface method (it also calls Close).
                try { ((IDisposable)_landmarker).Dispose(); } catch (Exception e) { Debug.LogException(e); }
                _landmarker = null;
            }
            if (_buffer.IsCreated) _buffer.Dispose();
            if (_wake != null) { _wake.Dispose(); _wake = null; }
            Volatile.Write(ref _busy, 0);
            _hasPending = false;
            _lastImageStamp = -1;
            LatestHands = NoHands;
            LatestSlots = NoSlots;
            _identity.Reset();
            HandCount = 0;
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
