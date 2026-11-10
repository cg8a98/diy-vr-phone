using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Room
{
    /// <summary>
    /// The only part that talks to ARCore. Adds an ARTrackedImageManager to the XR Origin that HeadPoseDriver built
    /// (if it has none), gives it a library made at run time from the two marker pictures, and reports where each
    /// marker is while ARCore is actively tracking it. Runs on the phone only; nothing here is saved or logged.
    /// </summary>
    internal sealed class MarkerTracker
    {
        private const float RetrySeconds = 0.5f;
        private const int MaxSubsystemTries = 40;
        private const float AddTimeoutSeconds = 30f;
        // The floor must be at least this far below marker A to count as the floor (not a table).
        private const float MinFloorDrop = 0.5f;

        private ARTrackedImageManager _manager;
        private ARPlaneManager _planes;
        private readonly Texture2D[] _textures = new Texture2D[RoomMarkers.Count];
        private readonly AddReferenceImageJobState[] _jobs = new AddReferenceImageJobState[RoomMarkers.Count];
        private readonly bool[] _jobDone = new bool[RoomMarkers.Count];
        private bool _setupDone;
        private bool _failed;
        private int _added;
        private int _tries;
        private float _nextTry;
        private float _setupTime;

        /// <summary>Short text for the status line while not ready, e.g. "waiting for AR".</summary>
        public string Status { get; private set; } = "waiting for AR";
        /// <summary>True once both marker pictures are in ARCore's library.</summary>
        public bool Ready { get { return _setupDone && !_failed && _added == RoomMarkers.Count; } }
        /// <summary>True if image tracking cannot work in this session; <see cref="Status"/> says why.</summary>
        public bool Failed { get { return _failed; } }

        public void Tick(float now)
        {
            if (_failed) return;
            if (!_setupDone)
            {
                if (now < _nextTry) return;
                _nextTry = now + RetrySeconds;
                try
                {
                    TrySetup(now);
                }
                catch (System.Exception e)
                {
                    Fail("image tracking did not start (" + e.GetType().Name + ")");
                }
                return;
            }
            if (_added < RoomMarkers.Count) PollJobs(now);
        }

        private void TrySetup(float now)
        {
            if (ARSession.state < ARSessionState.SessionInitializing) return;
            var origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin == null) return;

            if (_manager == null)
            {
                _manager = origin.GetComponent<ARTrackedImageManager>();
                // A new manager has no library yet, so it switches itself off; it is switched on again below.
                if (_manager == null) _manager = origin.gameObject.AddComponent<ARTrackedImageManager>();
            }
            if (_manager.subsystem == null)
            {
                _tries++;
                if (_tries >= MaxSubsystemTries) Fail("this phone offers no image tracking");
                return;
            }
            var descriptor = _manager.descriptor;
            if (descriptor == null || !descriptor.supportsMutableLibrary)
            {
                Fail("image tracking cannot add markers at run time");
                return;
            }
            var library = _manager.CreateRuntimeLibrary() as MutableRuntimeReferenceImageLibrary;
            if (library == null)
            {
                Fail("image tracking gave no marker library");
                return;
            }
            _manager.referenceLibrary = library;
            _manager.requestedMaxNumberOfMovingImages = RoomMarkers.Count;
            _manager.enabled = true;

            for (int i = 0; i < RoomMarkers.Count; i++)
            {
                _textures[i] = RoomMarkers.LoadTexture(i);
                if (_textures[i] == null)
                {
                    Fail("marker " + RoomMarkers.Letter(i) + " picture is missing from the app");
                    return;
                }
                // The texture must stay alive until the job has finished (see PollJobs).
                _jobs[i] = library.ScheduleAddImageWithValidationJob(_textures[i], RoomMarkers.Name(i), RoomMarkers.WidthMeters);
            }
            _setupDone = true;
            _setupTime = now;
            Status = "loading markers";
        }

        private void PollJobs(float now)
        {
            for (int i = 0; i < RoomMarkers.Count; i++)
            {
                if (_jobDone[i]) continue;
                AddReferenceImageJobStatus status = _jobs[i].status;
                if (status == AddReferenceImageJobStatus.Pending || status == AddReferenceImageJobStatus.None)
                {
                    if (now - _setupTime > AddTimeoutSeconds) Fail("marker " + RoomMarkers.Letter(i) + " took too long to load");
                    continue;
                }
                _jobDone[i] = true;
                RoomMarkers.DestroyNow(_textures[i]);
                _textures[i] = null;
                if (status == AddReferenceImageJobStatus.Success) _added++;
                else Fail("ARCore rejected marker " + RoomMarkers.Letter(i) + " (" + status + ")");
            }
            if (!_failed && _added == RoomMarkers.Count) Status = "ready";
        }

        private void Fail(string why)
        {
            _failed = true;
            Status = why;
            UnityEngine.Debug.LogWarning("Room frame: " + why);
        }

        /// <summary>World position of a marker's centre (0 = A, 1 = B), only while ARCore is tracking it right now.</summary>
        public bool TryGetMarker(int index, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (!Ready || _manager == null) return false;
            if (ARSession.state != ARSessionState.SessionTracking) return false;
            string name = RoomMarkers.Name(index);
            foreach (ARTrackedImage image in _manager.trackables)
            {
                // Limited = ARCore is only remembering where it last saw the marker. Not good enough.
                if (image.trackingState != TrackingState.Tracking) continue;
                if (image.referenceImage.name != name) continue;
                worldPosition = image.transform.position;
                return true;
            }
            return false;
        }

        /// <summary>World height of the lowest floor-like plane ARCore has found below <paramref name="markerY"/>.</summary>
        public bool TryGetFloorY(float markerY, out float floorWorldY)
        {
            floorWorldY = 0f;
            if (_planes == null) _planes = Object.FindFirstObjectByType<ARPlaneManager>();
            if (_planes == null) return false;
            bool found = false;
            foreach (ARPlane plane in _planes.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                if (plane.trackingState == TrackingState.None) continue;
                float y = plane.transform.position.y;
                if (y > markerY - MinFloorDrop) continue;
                if (!found || y < floorWorldY) floorWorldY = y;
                found = true;
            }
            return found;
        }
    }
}
