using System;
using System.IO;
using DiyVr.Stereo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiyVr.Room
{
    /// <summary>
    /// Milestone 3.1. Shows the owner's room scan in the stereo view.
    /// Controller: START (the small "menu" button with three lines) pressed briefly opens Android's file picker;
    /// the chosen .glb is copied into the app's private storage and shown. Holding START for about a second hides
    /// or shows the scan. The scan used last loads by itself when the app starts.
    /// The scan sits at the ARCore session origin (where the phone was when the app started), Y up, 1 unit = 1 m;
    /// lining it up with the real room is tickets 3.2 and 3.3.
    /// </summary>
    public sealed class ScanLoadController : MonoBehaviour
    {
        private const float LabelSeconds = 8f;
        private const float RigWaitSeconds = 5f;
        private const float LabelCharacterSize = 0.004f;
        private const int LabelFontSize = 64;
        /// <summary>Label position in front of the head: 1.2 m away, 0.3 m below eye level.</summary>
        public static readonly Vector3 LabelOffset = new Vector3(0f, -0.3f, 1.2f);

        private readonly ScanButtonLogic _button = new ScanButtonLogic();
        private ScanStore _store;
        private LoadedScan _scan;
        private bool _picking;
        private bool _loading;
        private bool _visible = true;
        private bool _destroyed;

        private TextMesh _label;
        private string _status = string.Empty;
        private float _statusUntil;
        private float _startTime;

        /// <summary>The scan now in the scene, or null.</summary>
        public LoadedScan Scan { get { return _scan; } }
        public string Status { get { return _status; } }

        private void Awake()
        {
            _store = new ScanStore(Application.persistentDataPath);
            _startTime = Time.unscaledTime;
        }

        private void Start()
        {
            string last = _store.LastScanPath();
            if (last != null) Load(last);
        }

        private void Update()
        {
            Gamepad pad = Gamepad.current;
            ScanButtonEvent ev = _button.Update(pad != null && pad.startButton.isPressed, Time.unscaledDeltaTime);
            if (ev == ScanButtonEvent.ShortPress) BeginPick();
            else if (ev == ScanButtonEvent.LongPress) ToggleVisible();

            if (_picking) PollPicker();
            UpdateLabel();
        }

        private void BeginPick()
        {
            if (_picking) return;
            if (_loading)
            {
                SetStatus("Scan: still loading, please wait");
                return;
            }
            string error;
            try
            {
                _store.PrepareIncoming();
            }
            catch (Exception e)
            {
                SetStatus(ScanLoadLogic.ErrorText(e.Message));
                return;
            }
            if (!ScanPicker.Start(_store.IncomingPath, out error))
            {
                SetStatus(ScanLoadLogic.ErrorText(error));
                return;
            }
            _picking = true;
            SetStatus("Scan: choose a .glb file on the phone screen");
        }

        private void PollPicker()
        {
            int state = ScanPicker.State();
            if (state == ScanPicker.Picking) return;
            _picking = false;

            if (state == ScanPicker.Done)
            {
                string pickedName = ScanPicker.PickedName();
                ScanPicker.Reset();
                string path, error;
                if (_store.TryImport(pickedName, out path, out error)) Load(path);
                else SetStatus(ScanLoadLogic.ErrorText(error));
            }
            else if (state == ScanPicker.Failed)
            {
                string error = ScanPicker.Error();
                ScanPicker.Reset();
                SetStatus(ScanLoadLogic.ErrorText(error));
            }
            else
            {
                ScanPicker.Reset();
                SetStatus("Scan: no file chosen");
            }
        }

        private void ToggleVisible()
        {
            if (_scan == null || _scan.Root == null)
            {
                SetStatus("Scan: none loaded (press START briefly to choose one)");
                return;
            }
            _visible = !_visible;
            _scan.Root.SetActive(_visible);
            SetStatus(_visible ? ScanLoadLogic.StatusText(_scan.Name, _scan.Triangles, _scan.TexturePx) : "Scan: hidden (hold START to show)");
        }

        private async void Load(string path)
        {
            if (_loading) return;
            _loading = true;
            string scanName = ScanStore.DisplayName(path);
            SetStatus("Scan: loading " + scanName + "...");
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (_scan != null)
                {
                    _scan.Unload();
                    _scan = null;
                }
                LoadedScan scan = await ScanSceneBuilder.BuildAsync(bytes, scanName, ScanLoadLogic.MaxTextureSize);
                if (_destroyed)
                {
                    scan.Unload();
                    return;
                }
                if (scan.Ok)
                {
                    _scan = scan;
                    _visible = true;
                    scan.Root.SetActive(true);
                    SetStatus(ScanLoadLogic.StatusText(scan.Name, scan.Triangles, scan.TexturePx));
                }
                else
                {
                    SetStatus(ScanLoadLogic.ErrorText(scan.Error));
                }
            }
            catch (Exception e)
            {
                SetStatus(ScanLoadLogic.ErrorText(e.Message));
            }
            finally
            {
                _loading = false;
            }
        }

        private void SetStatus(string text)
        {
            _status = ScanLoadLogic.Shorten(text, ScanLoadLogic.MaxLabelLength);
            _statusUntil = Time.unscaledTime + LabelSeconds;
            Debug.Log("[ScanLoad] " + text);
        }

        // The label hangs on the head (the stereo rig), so it stays in view; it shows for a few seconds after each change.
        private void UpdateLabel()
        {
            bool show = _status.Length > 0 && Time.unscaledTime < _statusUntil;
            if (_label == null)
            {
                if (!show) return;
                Transform head = FindHead();
                if (head == null) return;
                _label = BuildLabel(head);
            }
            if (_label.text != _status) _label.text = _status;
            if (_label.gameObject.activeSelf != show) _label.gameObject.SetActive(show);
        }

        private Transform FindHead()
        {
            StereoRig rig = FindFirstObjectByType<StereoRig>();
            if (rig != null) return rig.transform;
            // Give the rig a few seconds to appear before settling for the main camera.
            if (Time.unscaledTime - _startTime < RigWaitSeconds) return null;
            return Camera.main != null ? Camera.main.transform : null;
        }

        /// <summary>Builds the status label as a child of <paramref name="head"/>. Public so an EditMode test can run it.</summary>
        public static TextMesh BuildLabel(Transform head)
        {
            var go = new GameObject("ScanStatusLabel");
            go.transform.SetParent(head, false);
            go.transform.localPosition = LabelOffset;
            go.transform.localRotation = Quaternion.identity;

            var text = go.AddComponent<TextMesh>();
            // TextMesh requires a MeshRenderer and adds one itself; AddComponent<MeshRenderer>() would return null.
            var renderer = go.GetComponent<MeshRenderer>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.fontSize = LabelFontSize;
            text.characterSize = LabelCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            renderer.sharedMaterial = font.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }

        private void OnDestroy()
        {
            _destroyed = true;
            if (_scan != null)
            {
                _scan.Unload();
                _scan = null;
            }
            if (_label != null) Destroy(_label.gameObject);
        }
    }
}
