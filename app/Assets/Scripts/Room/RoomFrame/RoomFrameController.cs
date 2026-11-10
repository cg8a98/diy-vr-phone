using System.Collections.Generic;
using System.IO;
using DiyVr.Core;
using DiyVr.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiyVr.Room
{
    /// <summary>
    /// Ticket 3.2. Finds the two printed wall markers, works out the room frame from them and publishes it on the
    /// <see cref="RoomFrame"/> component of this same object (so this object's transform is the room).
    ///
    /// First time: as soon as both markers have been seen the frame is calibrated and saved on the phone.
    /// Every later start: look at marker A, then B, and the same frame comes back.
    ///
    /// Xbox controller (only while the lens tool is closed, where the D-pad means something else):
    ///   D-pad LEFT, short press  - show / hide the room gizmo and status text
    ///   D-pad LEFT, hold 1 second - calibrate again (after moving a marker)
    ///   D-pad RIGHT              - drop the test object where the phone is now (saved, comes back after a restart)
    ///
    /// Nothing here is logged or sent anywhere: marker and room data only go into the private file on the phone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomFrameController : MonoBehaviour
    {
        public const float CalibrateHoldSeconds = 1f;
        /// <summary>Above this marker-check value the status text warns that a marker may have moved.</summary>
        public const float ResidualWarnMeters = 0.03f;
        private const float NoteSeconds = 6f;

        private RoomFrameSolver _solver;
        private RoomFrameData _data;
        private RoomFrame _frame;
        private MarkerTracker _tracker;
        private string _path;
        private bool _inited;
        private RoomSolution _solution;
        private bool _hasSolution;

        private readonly List<Material> _materials = new List<Material>();
        private GameObject _gizmo;
        private GameObject _markerBPoint;
        private GameObject _testObject;
        private TextMesh _hud;
        private Transform _head;
        private bool _shown = true;
        private string _hudText = string.Empty;

        private string _note = string.Empty;
        private float _noteUntil;
        private float _leftDownAt = -1f;
        private bool _leftFired;

        public RoomFrame Frame { get { return _frame; } }
        /// <summary>The saved data (calibration and test object). Private to the phone.</summary>
        public RoomFrameData Data { get { return _data; } }
        public bool Shown { get { return _shown; } }
        public GameObject Gizmo { get { return _gizmo; } }
        public GameObject TestObject { get { return _testObject; } }
        public GameObject MarkerBPoint { get { return _markerBPoint; } }
        public TextMesh Hud { get { return _hud; } }

        private void Awake()
        {
            if (!Application.isPlaying) return; // EditMode tests call Init themselves with a temporary file
            Init(Path.Combine(Application.persistentDataPath, RoomFrameData.FileName));
        }

        /// <summary>
        /// Loads the saved file and builds the visible objects. Awake calls it with the phone's private folder; an
        /// EditMode test calls it with a temporary file. Does nothing the second time.
        /// </summary>
        public void Init(string storagePath)
        {
            if (_inited) return;
            _inited = true;
            _path = storagePath;
            _solver = new RoomFrameSolver();
            _data = RoomFrameData.Load(_path);
            _frame = GetComponent<RoomFrame>();
            if (_frame == null) _frame = gameObject.AddComponent<RoomFrame>();
            _frame.Register();
            BuildVisuals();
            RefreshVisuals();
            RefreshHud(0f);
        }

        private void BuildVisuals()
        {
            _gizmo = RoomFrameVisuals.BuildGizmo(transform, _materials);
            _markerBPoint = RoomFrameVisuals.BuildPoint(transform, "Marker B Point", "marker B", new Color(0.3f, 0.5f, 1f), 0.03f, _materials);
            _testObject = RoomFrameVisuals.BuildPoint(transform, "Room Test Object", "TEST", new Color(1f, 0.8f, 0.1f), 0.06f, _materials);
            _hud = RoomFrameVisuals.BuildHud();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (_tracker == null) _tracker = new MarkerTracker();
            _tracker.Tick(now);
            for (int i = 0; i < RoomMarkers.Count; i++)
            {
                Vector3 p;
                if (_tracker.TryGetMarker(i, out p)) FeedMarker(i, p, now);
            }
            FindHead();
            HandlePad(now);
            RefreshHud(now);
        }

        private void FindHead()
        {
            if (_head != null) return;
            // The rig (HeadPoseDriver's object) is the head; fall back to the main camera.
            var driver = FindFirstObjectByType<HeadPoseDriver>();
            if (driver != null) _head = driver.transform;
            else if (Camera.main != null) _head = Camera.main.transform;
            if (_head != null && _hud != null) RoomFrameVisuals.AttachHud(_hud, _head);
        }

        /// <summary>
        /// One sighting of a marker (0 = A, 1 = B) at a world position. Update feeds this from ARCore; a test feeds
        /// it directly. Once both markers have enough good sightings the frame is set (and refined from then on).
        /// </summary>
        public void FeedMarker(int marker, Vector3 worldPosition, float time)
        {
            if (!_solver.AddSample(marker, worldPosition, time)) return;
            RoomSolution s;
            if (!_solver.TrySolve(out s)) return;
            _solution = s;
            _hasSolution = true;
            if (!_data.calibrated) Calibrate();
            else Apply();
        }

        /// <summary>
        /// Saves the marker layout seen right now as "the room". Later starts are checked against it.
        /// False if the markers have not both been seen yet this session.
        /// </summary>
        public bool Calibrate()
        {
            if (!_hasSolution)
            {
                Note("Look at marker A, then marker B, first");
                return false;
            }
            _data.calibrated = true;
            _data.markerWidth = RoomMarkers.WidthMeters;
            _data.markerBInRoom = _solution.MarkerBInRoom;
            float floorWorldY;
            if (_tracker != null && _tracker.TryGetFloorY(_solution.Pose.Origin.y, out floorWorldY))
            {
                _data.hasFloor = true;
                _data.floorY = floorWorldY - _solution.Pose.Origin.y;
            }
            else
            {
                _data.hasFloor = false;
                _data.floorY = 0f;
            }
            bool saved = _data.Save(_path);
            Apply();
            Note(saved ? "Calibrated and saved" : "Calibrated, but saving FAILED");
            return true;
        }

        private void Apply()
        {
            float residual = (_solution.MarkerBInRoom - _data.markerBInRoom).magnitude;
            _frame.SetLock(_solution.Pose, residual, _solution.Spread, _data.hasFloor, _data.floorY);
            RefreshVisuals();
        }

        /// <summary>
        /// Puts the test object at a world position and saves it in room coordinates. False until the frame is locked.
        /// </summary>
        public bool DropTestObject(Vector3 worldPosition)
        {
            if (!_frame.IsLocked)
            {
                Note("Room not locked yet: look at marker A, then B");
                return false;
            }
            _data.hasTestObject = true;
            _data.testObject = _frame.WorldToRoom(worldPosition);
            bool saved = _data.Save(_path);
            RefreshVisuals();
            Note(saved ? "Test object saved here" : "Test object placed, but saving FAILED");
            return true;
        }

        /// <summary>Shows or hides the gizmo, the test object and the status text.</summary>
        public void SetShown(bool shown)
        {
            _shown = shown;
            RefreshVisuals();
        }

        private void HandlePad(float now)
        {
            var pad = Gamepad.current;
            if (pad == null || GestureArbiter.Shared.LensToolOpen)
            {
                _leftDownAt = -1f;
                return;
            }
            if (pad.dpad.right.wasPressedThisFrame && _head != null) DropTestObject(_head.position);

            if (pad.dpad.left.wasPressedThisFrame)
            {
                _leftDownAt = now;
                _leftFired = false;
            }
            if (_leftDownAt < 0f) return;
            if (pad.dpad.left.isPressed)
            {
                if (!_leftFired && now - _leftDownAt >= CalibrateHoldSeconds)
                {
                    _leftFired = true;
                    SetShown(true);
                    Calibrate();
                }
            }
            else
            {
                if (!_leftFired) SetShown(!_shown);
                _leftDownAt = -1f;
            }
        }

        private void RefreshVisuals()
        {
            bool locked = _frame != null && _frame.IsLocked;
            if (_gizmo != null) _gizmo.SetActive(_shown && locked);
            if (_markerBPoint != null)
            {
                _markerBPoint.SetActive(_shown && locked);
                _markerBPoint.transform.localPosition = _data.markerBInRoom;
            }
            if (_testObject != null)
            {
                _testObject.SetActive(_shown && locked && _data.hasTestObject);
                _testObject.transform.localPosition = _data.testObject;
            }
            if (_hud != null) _hud.gameObject.SetActive(_shown);
        }

        private void RefreshHud(float now)
        {
            if (_hud == null || !_shown) return;
            string text = StatusText(now);
            if (text == _hudText) return;
            _hudText = text;
            _hud.text = text;
        }

        /// <summary>The status text: what the room frame is doing and how far the phone is from the test object.</summary>
        public string StatusText(float now)
        {
            string text;
            if (_frame.IsLocked)
            {
                text = "Room: LOCKED   steadiness " + Cm(_frame.SpreadMeters) + "   marker check " + Cm(_frame.ResidualMeters);
                if (_frame.ResidualMeters > ResidualWarnMeters) text += "\nA marker moved? Hold D-pad LEFT 1 s to calibrate again";
                if (_data.hasTestObject)
                {
                    if (_head != null)
                        text += "\nTest object: " + Cm(Vector3.Distance(_head.position, _frame.RoomToWorld(_data.testObject))) + " from the phone";
                }
                else
                {
                    text += "\nD-pad RIGHT: drop a test object where the phone is";
                }
            }
            else if (_tracker != null && !_tracker.Ready)
            {
                text = "Room: " + _tracker.Status;
            }
            else
            {
                text = (_data.calibrated ? "Room: look at marker A, then B" : "Room: not calibrated. Look at marker A, then B")
                    + "\nA " + _solver.SampleCount(0) + "/" + _solver.WindowSize + "   B " + _solver.SampleCount(1) + "/" + _solver.WindowSize;
            }
            if (_note.Length > 0 && now < _noteUntil) text += "\n" + _note;
            return text;
        }

        private static string Cm(float meters)
        {
            return (meters * 100f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " cm";
        }

        private void Note(string text)
        {
            _note = text;
            _noteUntil = (Application.isPlaying ? Time.unscaledTime : 0f) + NoteSeconds;
        }

        private void OnDestroy()
        {
            Teardown();
        }

        /// <summary>Frees the materials and the status text. OnDestroy calls it; EditMode tests call it themselves.</summary>
        public void Teardown()
        {
            for (int i = 0; i < _materials.Count; i++) RoomMarkers.DestroyNow(_materials[i]);
            _materials.Clear();
            // The status text hangs under the head, not under this object.
            if (_hud != null) RoomMarkers.DestroyNow(_hud.gameObject);
            _hud = null;
        }
    }
}
