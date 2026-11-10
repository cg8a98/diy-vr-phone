using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>Adds the hand drawing without touching AppRoot (same pattern as GazeBootstrap).</summary>
    public static class HandViewBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (!Application.isPlaying) return;
            if (Object.FindFirstObjectByType<HandViewManager>() != null) return;
            var go = new GameObject("Hand View");
            go.AddComponent<HandViewManager>();
        }
    }

    /// <summary>
    /// Finds the HandTrackingService (it may be created after this runs), makes two HandViews and hands each new result
    /// to them. Slots follow MediaPipe's Left/Right label when the two hands differ, otherwise result order.
    /// </summary>
    [DefaultExecutionOrder(-50)] // after HandTrackingService (-100) has published, before the views draw
    public sealed class HandViewManager : MonoBehaviour
    {
        private HandTrackingService _service;
        private HandView[] _views;
        private int _lastVersion;
        private HandLagProbe _probe;

        private void Update()
        {
            if (_service == null)
            {
                _service = FindFirstObjectByType<HandTrackingService>();
                if (_service == null) return;
            }
            if (_views == null)
            {
                _probe = gameObject.AddComponent<HandLagProbe>();
                _views = new HandView[2];
                Color[] colours = { new Color(0f, 0.9f, 1f), Color.white };
                for (int i = 0; i < 2; i++)
                {
                    var go = new GameObject(i == 0 ? "Hand A" : "Hand B");
                    go.transform.SetParent(transform, false);
                    _views[i] = go.AddComponent<HandView>();
                    _views[i].Build(colours[i]);
                }
            }

            int version = _service.ResultVersion;
            if (version == _lastVersion) return;
            _lastVersion = version;

            var hands = _service.LatestHands;
            HandFrameInfo frame = _service.LatestFrame;
            if (_probe != null) _probe.NewResult(frame.CaptureSeconds);
            float now = Time.unscaledTime;
            bool bySide = hands.Count == 2 && hands[0].Side != hands[1].Side
                && hands[0].Side != HandSide.Unknown && hands[1].Side != HandSide.Unknown;
            for (int i = 0; i < hands.Count && i < 2; i++)
            {
                int slot = bySide ? (hands[i].Side == HandSide.Left ? 0 : 1) : i;
                if (hands.Count == 1 && hands[0].Side == HandSide.Right) slot = 1;
                _views[slot].Apply(hands[i], frame, now);
            }
        }
    }

    /// <summary>
    /// Runs after every HandView has drawn this frame. When a new result was drawn this frame it records
    /// camera-to-draw time (now minus the camera frame's exposure time) for the debug overlay. Display scan-out
    /// after the draw (up to one more refresh) is not included.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class HandLagProbe : MonoBehaviour
    {
        private readonly RollingAverage _total = new RollingAverage(20);
        private double _pendingCapture;
        private bool _has;

        public void NewResult(double captureSeconds)
        {
            _pendingCapture = captureSeconds;
            _has = captureSeconds > 0;
        }

        private void LateUpdate()
        {
            if (!_has) return;
            _has = false;
            double ms = (HandClock.Now() - _pendingCapture) * 1000.0;
            if (ms < 0 || ms > 2000) return;
            _total.Add(ms);
            HandsStatus.LagTotalMs = _total.Average;
        }
    }
}
