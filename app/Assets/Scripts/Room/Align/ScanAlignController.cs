using System.IO;
using DiyVr.Core;
using DiyVr.Passthrough;
using DiyVr.Stereo;
using DiyVr.Updater;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiyVr.Room
{
    /// <summary>
    /// Milestone 3.3. Hangs the scan on the room frame: after one manual alignment the scan comes back on the
    /// real walls on every start, as soon as the two markers have been seen.
    ///
    /// Controller:
    ///   hold D-pad UP for 1 s (lens tool closed) = open align mode. Needs a loaded scan and a locked room frame.
    ///   In align mode: left stick = slide the scan along the floor, relative to where you look;
    ///   right stick left/right = turn it around the spot where you stand; right trigger = up, left trigger = down;
    ///   hold LB or RB = ten times slower (fine); A = save and close; B = cancel (the scan goes back).
    /// While align mode is open the scan is see-through, the camera view is forced on (and put back afterwards),
    /// and the other controller features are switched off (see <see cref="ControllerModal"/>).
    ///
    /// What is saved: the scan's pose in room coordinates, per scan file, in the app's private storage on the
    /// phone. Before a scan was ever aligned it is put in front of you on the floor as a starting guess.
    /// Poses are never written to the log.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScanAlignController : MonoBehaviour
    {
        private const float LabelSeconds = 8f;
        private const float SearchInterval = 0.5f;
        public const string Hint = "ALIGN MODE\nL stick move, R stick turn, triggers up/down\nhold LB/RB fine, A save, B cancel";

        private readonly AlignHoldLogic _hold = new AlignHoldLogic();
        private readonly AlignButtons _buttons = new AlignButtons();
        private readonly ScanGhost _ghost = new ScanGhost();

        private ScanAlignStore _store;
        private ScanLoadController _loader;
        private PassthroughController _passthrough;
        private bool _passthroughWasOn;
        private Transform _head;
        private float _nextSearch;

        private GameObject _root;
        private string _key;
        private Bounds _bounds;
        private bool _hasSaved;
        private ScanPose _saved;   // scan in room coordinates
        private ScanPose _world;   // scan in the world now
        private ScanPose _before;  // scan in the world when align mode opened
        private bool _placed;
        private bool _aligning;

        private TextMesh _label;
        private string _status = string.Empty;
        private string _message = string.Empty;
        private float _statusUntil;

        public bool IsAligning { get { return _aligning; } }
        /// <summary>True if this scan has a saved alignment.</summary>
        public bool HasSaved { get { return _hasSaved; } }
        /// <summary>The saved scan-in-room pose. Only valid if <see cref="HasSaved"/>.</summary>
        public ScanPose SavedPose { get { return _saved; } }
        /// <summary>Where the scan is in the world now.</summary>
        public ScanPose WorldPose { get { return _world; } }
        public GameObject ScanRoot { get { return _root; } }
        public ScanGhost Ghost { get { return _ghost; } }
        public TextMesh Label { get { return _label; } }
        public string Status { get { return _status; } }

        /// <summary>Opens the saved poses. Called by Update on the phone; tests call it with their own file.</summary>
        public void Init(string storagePath)
        {
            _store = new ScanAlignStore(storagePath);
        }

        /// <summary>The head (stereo rig): moving is relative to where it looks, and the label hangs on it.</summary>
        public void SetHead(Transform head)
        {
            _head = head;
        }

        private void Update()
        {
            EnsureInit();
            FindThings();
            SyncScan();
            Place();
            HandlePad(Time.unscaledDeltaTime);
            RefreshLabel(Time.unscaledTime);
        }

        private void EnsureInit()
        {
            if (_store == null) Init(Path.Combine(Application.persistentDataPath, ScanAlignStore.FileName));
        }

        private void FindThings()
        {
            if (_head != null && _loader != null) return;
            if (Time.unscaledTime < _nextSearch) return;
            _nextSearch = Time.unscaledTime + SearchInterval;
            if (_loader == null) _loader = FindFirstObjectByType<ScanLoadController>();
            if (_head == null)
            {
                StereoRig rig = FindFirstObjectByType<StereoRig>();
                if (rig != null) _head = rig.transform;
            }
        }

        private void SyncScan()
        {
            if (_aligning && _root == null) EndAlign(); // the scan was removed while aligning
            if (_loader == null) return;
            LoadedScan scan = _loader.Scan;
            GameObject root = scan != null ? scan.Root : null;
            if (root == _root) return;
            Bind(root, scan != null ? scan.Name : null);
        }

        /// <summary>Tells the controller which scan is in the scene now (null = none).</summary>
        public void Bind(GameObject scanRoot, string scanName)
        {
            EnsureInit();
            if (_aligning) EndAlign(); // the scan was replaced or removed while aligning
            _root = scanRoot;
            _placed = false;
            _hasSaved = false;
            if (_root == null) return;
            _key = ScanAlignStore.SafeKey(scanName);
            _hasSaved = _store.TryGet(_key, out _saved);
            _bounds = ScanAlignVisuals.LocalBounds(_root);
            _world = new ScanPose(_root.transform.position, _root.transform.eulerAngles.y);
            Say(null, Time.unscaledTime);
        }

        /// <summary>
        /// Puts the scan where it belongs: on the saved pose once the room frame is locked, otherwise (once) in
        /// front of the head on the floor as a starting guess. Does nothing while aligning.
        /// </summary>
        public void Place()
        {
            if (_root == null || _aligning) return;
            RoomFrame frame = RoomFrame.Current;
            bool locked = frame != null && frame.IsLocked;
            if (_hasSaved && locked)
            {
                Apply(ScanAlignMath.RoomToWorld(frame.Pose, _saved));
                _placed = true;
                return;
            }
            if (_placed || _head == null) return;

            // The floor from the room frame if it knows it, else a guess from the eye height.
            float floorY = locked && frame.HasFloor
                ? frame.RoomToWorld(new Vector3(0f, frame.FloorY, 0f)).y
                : _head.position.y - ScanAlignMath.GuessEyeHeight;
            Apply(ScanAlignMath.StartingGuess(_bounds, _head.position, LookYaw(), floorY));
            _placed = true;
        }

        private void HandlePad(float deltaTime)
        {
            Gamepad pad = Gamepad.current;
            if (pad == null || GestureArbiter.Shared.LensToolOpen || UpdatePrompt.IsOpen)
            {
                _hold.Reset();
                _buttons.Block();
                return;
            }

            if (!_aligning)
            {
                if (_hold.Update(pad.dpad.up.isPressed, deltaTime)) EnterAlign();
                _buttons.Block();
                return;
            }

            bool save, cancel;
            _buttons.Update(pad.buttonSouth.isPressed, pad.buttonEast.isPressed, out save, out cancel);
            if (save) { SaveAlign(); return; }
            if (cancel) { CancelAlign(); return; }

            var input = new AlignInput
            {
                Move = pad.leftStick.ReadValue(),
                Turn = pad.rightStick.ReadValue().x,
                Lift = pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue(),
                Fine = pad.leftShoulder.isPressed || pad.rightShoulder.isPressed,
            };
            StepAlign(input, deltaTime);
        }

        /// <summary>Opens align mode. False (with the reason in the status line) if that is not possible yet.</summary>
        public bool EnterAlign()
        {
            if (_aligning) return true;
            float now = Time.unscaledTime;
            if (_root == null)
            {
                Say("Align: no scan loaded (press START to choose one)", now);
                return false;
            }
            if (!_root.activeInHierarchy)
            {
                Say("Align: the scan is hidden (hold START to show it)", now);
                return false;
            }
            RoomFrame frame = RoomFrame.Current;
            if (frame == null || !frame.IsLocked)
            {
                Say("Align: look at both wall markers first (frame not locked)", now);
                return false;
            }

            _world = new ScanPose(_root.transform.position, _root.transform.eulerAngles.y);
            _before = _world;
            _aligning = true;
            ControllerModal.IsOpen = true;
            _buttons.Block();
            _ghost.Show(_root);

            // Camera view on while aligning; remember whether the owner had it on.
            if (_passthrough == null) _passthrough = FindFirstObjectByType<PassthroughController>();
            if (_passthrough != null)
            {
                _passthroughWasOn = _passthrough.Mode == PassthroughMode.ManualOn;
                _passthrough.SetManual(true);
            }
            Say(null, now);
            Debug.Log("[Align] align mode opened");
            return true;
        }

        /// <summary>One frame of stick movement in align mode.</summary>
        public void StepAlign(AlignInput input, float deltaTime)
        {
            if (!_aligning || _root == null) return;
            Vector3 pivot = _head != null ? _head.position : _world.Position;
            Apply(ScanAlignMath.Step(_world, input, LookYaw(), pivot, deltaTime));
        }

        /// <summary>Saves where the scan is now (in room coordinates) and closes align mode. False if it could not be saved.</summary>
        public bool SaveAlign()
        {
            if (!_aligning) return false;
            float now = Time.unscaledTime;
            RoomFrame frame = RoomFrame.Current;
            if (frame == null || !frame.IsLocked)
            {
                Say("Align: NOT saved, the room frame is not locked (look at both markers)", now);
                return false;
            }
            ScanPose inRoom = ScanAlignMath.WorldToRoom(frame.Pose, _world);
            string error;
            if (!_store.Set(_key, inRoom, out error))
            {
                Say("Align: NOT saved, could not write the file", now);
                Debug.LogWarning("[Align] could not save the alignment: " + error);
                return false;
            }
            _saved = inRoom;
            _hasSaved = true;
            EndAlign();
            Say(null, now);
            Debug.Log("[Align] alignment saved");
            return true;
        }

        /// <summary>Closes align mode and puts the scan back where it was when align mode opened.</summary>
        public void CancelAlign()
        {
            if (!_aligning) return;
            if (_root != null) Apply(_before);
            EndAlign();
            Say(null, Time.unscaledTime);
            Debug.Log("[Align] align mode cancelled");
        }

        private void EndAlign()
        {
            _aligning = false;
            ControllerModal.IsOpen = false;
            _ghost.Restore();
            _hold.Reset();
            if (_passthrough != null && !_passthroughWasOn) _passthrough.SetManual(false);
        }

        private void Apply(ScanPose world)
        {
            _world = world;
            if (_root != null) _root.transform.SetPositionAndRotation(world.Position, world.Rotation);
        }

        private float LookYaw()
        {
            return _head != null ? ScanAlignMath.LookYaw(_head.forward, _head.up) : 0f;
        }

        /// <summary>"Align: saved / not saved, frame locked yes / no, marker residual x cm".</summary>
        public string StatusText()
        {
            RoomFrame frame = RoomFrame.Current;
            bool locked = frame != null && frame.IsLocked;
            return ScanAlignMath.StatusText(_hasSaved, locked, locked ? frame.ResidualMeters : 0f);
        }

        // message = a one-off line (why something did not work); null = just show the status line again.
        private void Say(string message, float now)
        {
            _message = message ?? string.Empty;
            _statusUntil = now + LabelSeconds;
        }

        /// <summary>
        /// Updates the label on the head: always shown in align mode (with the controls), otherwise for a few
        /// seconds after something changed. Public so an EditMode test can run it.
        /// </summary>
        public void RefreshLabel(float now)
        {
            string text = _message.Length > 0 ? _message : StatusText();
            if (_aligning) text = text + "\n" + Hint;
            _status = text;

            bool show = _aligning || now < _statusUntil;
            if (_label == null)
            {
                if (!show || _head == null) return;
                _label = ScanAlignVisuals.BuildLabel(_head);
            }
            if (_label.text != text) _label.text = text;
            if (_label.gameObject.activeSelf != show) _label.gameObject.SetActive(show);
        }

        /// <summary>Closes align mode and removes the label. OnDestroy calls it; tests call it too (OnDestroy does not run in EditMode).</summary>
        public void Teardown()
        {
            if (_aligning) EndAlign();
            if (_label != null) ScanAlignVisuals.DestroyObject(_label.gameObject);
            _label = null;
        }

        private void OnDestroy()
        {
            Teardown();
        }
    }
}
