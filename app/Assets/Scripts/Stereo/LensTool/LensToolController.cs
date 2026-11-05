using System.Collections.Generic;
using System.IO;
using System.Text;
using DiyVr.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DiyVr.Stereo.LensTool
{
    /// <summary>
    /// In-headset lens tuning screen. A large world-space panel is parented to the rig, so both eye cameras draw it
    /// (it shows in both halves of the screen). Touch only:
    ///   tap left half = previous row, tap right half = next row, swipe up/down = change value,
    ///   hold = save, two-finger tap = open/close the tool.
    /// Every change rebuilds the distortion immediately. Also usable from code through the public methods.
    /// The last used profile is loaded at start even when the tool is closed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LensToolController : MonoBehaviour
    {
        public StereoRig rig;
        public bool startOpen;
        [Tooltip("Distance in front of the head, metres.")]
        public float panelDistance = 0.8f;
        [Tooltip("Panel width in metres at that distance.")]
        public float panelWidth = 0.7f;

        private const int RowProfile = 0;                       // row 0 = profile chooser, rows 1.. = LensParameters.All
        private static int RowCount { get { return LensParameters.All.Length + 1; } }
        private const string NewSlot = "+ new copy";

        private LensProfileStore _store;
        private LensProfile _working;
        private string _profileName;
        private int _row;
        private bool _open;
        private bool _gridSuppressed;
        private bool _dirty;
        private string _status = "";
        private float _statusUntil;
        private GestureRecognizer _gestures;
        private GameObject _panel, _grid;
        private Text _text;
        private List<string> _names = new List<string>();
        private int _nameIndex;                                  // index in _names; _names.Count = "+ new copy"

        public bool IsOpen { get { return _open; } }
        public string ProfileName { get { return _profileName; } }
        public int SelectedRow { get { return _row; } }
        public LensProfile Working { get { return _working; } }

        /// <summary>Adds the tool to any scene that has a StereoRig, so no scene editing is needed.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            var r = FindFirstObjectByType<StereoRig>();
            if (r != null && FindFirstObjectByType<LensToolController>() == null)
                r.gameObject.AddComponent<LensToolController>().rig = r;
        }

        private void Start()
        {
            if (rig == null) rig = FindFirstObjectByType<StereoRig>();
            if (rig == null) { enabled = false; return; }

            _store = new LensProfileStore(new PhysicalFileStore(), Path.Combine(Application.persistentDataPath, LensProfileStore.FolderName));
            _working = Instantiate(rig.ActiveProfile);
            try { _profileName = _store.LoadStartup(_working); }
            catch (System.Exception e) { _profileName = LensProfileStore.DefaultName; Debug.LogWarning("Lens profile storage unavailable: " + e.Message); }
            rig.ApplyProfile(_working);
            _gestures = new GestureRecognizer(Screen.width, Screen.height);
            RefreshNames();
            if (startOpen) Open();
        }

        private void OnDestroy()
        {
            if (_open) GestureArbiter.Shared.LensToolOpen = false;
            if (_working != null) Destroy(_working);
            if (_panel != null) Destroy(_panel);
            if (_grid != null) Destroy(_grid);
        }

        private void Update()
        {
            if (_gestures == null) return;
            _gestures.Resize(Screen.width, Screen.height);
            var g = _gestures.Update(ReadTouch());
            if (g == GestureKind.TwoFingerTap)
            {
                if (GestureArbiter.Shared.Allows(GestureTarget.LensTool, TouchGesture.TwoFingerTap)) { if (_open) Close(); else Open(); }
                return;
            }
            if (!_open) return;
            switch (g)
            {
                case GestureKind.TapLeft: SelectPrevious(); break;
                case GestureKind.TapRight: SelectNext(); break;
                case GestureKind.StepUp: Adjust(1); break;
                case GestureKind.StepDown: Adjust(-1); break;
                case GestureKind.LongPress: Save(); break;
            }
            if (_status.Length > 0 && Time.unscaledTime > _statusUntil) { _status = ""; UpdateText(); }
        }

        private static TouchFrame ReadTouch()
        {
            var f = new TouchFrame { Time = Time.unscaledTime };
            var ts = Touchscreen.current;
            if (ts != null)
            {
                int n = 0;
                foreach (var t in ts.touches) if (t.isInProgress) n++;
                if (n > 0)
                {
                    var p = ts.primaryTouch.position.ReadValue();
                    f.Count = n; f.X = p.x; f.Y = p.y;
                    return f;
                }
            }
            var m = Mouse.current;   // lets the tool be tried in the Unity editor
            if (m != null && m.leftButton.isPressed)
            {
                var p = m.position.ReadValue();
                f.Count = 1; f.X = p.x; f.Y = p.y;
            }
            return f;
        }

        // ---------- public API ----------

        public void Open()
        {
            if (_working == null) return;
            _open = true;
            GestureArbiter.Shared.LensToolOpen = true;
            EnsurePanel();
            _panel.SetActive(true);
            ApplyGridVisibility();
            RefreshNames();
            UpdateText();
        }

        public void Close()
        {
            _open = false;
            GestureArbiter.Shared.LensToolOpen = false;
            if (_panel != null) _panel.SetActive(false);
            ApplyGridVisibility();
        }

        /// <summary>
        /// Hides the lens tool grid while the camera view is on (the grid is opaque and would cover the camera image).
        /// The panel stays visible. Safe to call before the grid exists; it applies when the tool opens.
        /// </summary>
        public void SetGridSuppressed(bool suppressed)
        {
            _gridSuppressed = suppressed;
            ApplyGridVisibility();
        }

        private void ApplyGridVisibility()
        {
            if (_grid != null) _grid.SetActive(SceneVisibility.LensGridVisible(_gridSuppressed, _open));
        }

        public void SelectNext() { _row = (_row + 1) % RowCount; UpdateText(); }

        public void SelectPrevious() { _row = (_row + RowCount - 1) % RowCount; UpdateText(); }

        /// <summary>Changes the selected row one step: +1 up, -1 down. On the profile row it switches profile.</summary>
        public void Adjust(int direction)
        {
            if (_working == null || direction == 0) return;
            if (_row == RowProfile) { CycleProfile(direction > 0 ? 1 : -1); return; }
            LensParameters.All[_row - 1].StepBy(_working, direction);
            _dirty = true;
            rig.Rebuild();
            UpdateText();
        }

        /// <summary>Sets one parameter by its on-screen unit (mm, degrees) and rebuilds.</summary>
        public void SetParameter(LensParameter id, double displayValue)
        {
            if (_working == null) return;
            LensParameters.Get(id).Set(_working, displayValue);
            _dirty = true;
            rig.Rebuild();
            UpdateText();
        }

        /// <summary>Saves under the current profile name, or as a new "headset-N" copy when the chooser sits on "+ new copy".</summary>
        public void Save()
        {
            if (_working == null) return;
            bool wantNew = _row == RowProfile && _nameIndex >= _names.Count;
            SaveAs(wantNew ? _store.NextFreeName("headset") : _profileName);
        }

        public void SaveAs(string name)
        {
            if (_working == null) return;
            try
            {
                _profileName = _store.Save(name, _working);
                _store.SetLastUsed(_profileName);
                _dirty = false;
                RefreshNames();
                SetStatus("Saved " + _profileName);
            }
            catch (System.Exception e) { SetStatus("Save failed: " + e.Message); }
        }

        public bool LoadProfile(string name)
        {
            if (_working == null) return false;
            if (!_store.TryLoad(name, _working)) { SetStatus("Could not load " + name); return false; }
            _profileName = LensProfileStore.SafeName(name);
            _store.SetLastUsed(_profileName);
            _dirty = false;
            rig.Rebuild();
            RefreshNames();
            SetStatus("Loaded " + _profileName);
            return true;
        }

        // ---------- internals ----------

        private void RefreshNames()
        {
            _names = _store != null ? _store.List() : new List<string>();
            int i = _names.IndexOf(_profileName);
            _nameIndex = i >= 0 ? i : _names.Count;
        }

        private void CycleProfile(int dir)
        {
            int slots = _names.Count + 1;                      // saved profiles + "+ new copy"
            _nameIndex = (_nameIndex + dir + slots) % slots;
            if (_nameIndex < _names.Count) LoadProfile(_names[_nameIndex]);
            else { SetStatus("Hold to save as a new profile"); }
        }

        private void SetStatus(string s)
        {
            _status = s;
            _statusUntil = Time.unscaledTime + 3f;
            UpdateText();
        }

        private void UpdateText()
        {
            if (_text == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("LENS TUNING" + (_dirty ? "   (unsaved)" : ""));
            for (int r = 0; r < RowCount; r++)
            {
                string label, value;
                if (r == RowProfile)
                {
                    label = "Profile";
                    value = _nameIndex < _names.Count ? _names[_nameIndex] : NewSlot;
                }
                else
                {
                    var spec = LensParameters.All[r - 1];
                    label = spec.Label;
                    value = spec.Format(_working);
                }
                sb.AppendLine((r == _row ? "> " : "   ") + label + ":  " + value);
            }
            sb.AppendLine();
            sb.AppendLine(_status.Length > 0 ? _status : "Tap left/right: choose   Swipe: change");
            sb.Append("Hold: save   Two fingers: close");
            _text.text = sb.ToString();
        }

        private void EnsurePanel()
        {
            if (_panel != null) return;
            var head = rig.transform;

            _panel = new GameObject("LensToolPanel");
            _panel.transform.SetParent(head, false);
            _panel.transform.localPosition = new Vector3(0f, 0f, panelDistance);
            var canvas = _panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = _panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 640f);
            rt.localScale = Vector3.one * (panelWidth / 1000f);

            var bg = new GameObject("Background");
            bg.transform.SetParent(_panel.transform, false);
            var img = bg.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.82f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var tx = new GameObject("Text");
            tx.transform.SetParent(_panel.transform, false);
            _text = tx.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 46;
            _text.color = Color.white;
            _text.alignment = TextAnchor.MiddleLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            var txRt = tx.GetComponent<RectTransform>();
            txRt.anchorMin = Vector2.zero; txRt.anchorMax = Vector2.one;
            txRt.offsetMin = new Vector2(40f, 20f); txRt.offsetMax = new Vector2(-40f, -20f);

            // Test grid behind the panel: straight lines must look straight at the edges.
            _grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _grid.name = "LensToolGrid";
            var col = _grid.GetComponent<Collider>();
            if (col != null) Destroy(col);
            _grid.transform.SetParent(head, false);
            _grid.transform.localPosition = new Vector3(0f, 0f, 2.4f);
            _grid.transform.localScale = new Vector3(6f, 3f, 1f);
            var shader = rig.unlitShader != null ? rig.unlitShader : Shader.Find("DiyVr/StereoUnlit");
            var mat = new Material(shader) { mainTexture = StereoTestContent.MakeGridTexture(1024, 512, 16) };
            _grid.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
