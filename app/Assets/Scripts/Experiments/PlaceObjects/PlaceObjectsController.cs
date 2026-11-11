using System.Collections.Generic;
using DiyVr.Gestures;
using DiyVr.Hands;
using DiyVr.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DiyVr.Experiments.PlaceObjects
{
    /// <summary>
    /// Milestone 2.6. Looks along the head's forward direction against the detected AR planes (ARRaycastManager); a
    /// yellow disc shows where an object would land. A thumb+index pinch drops an 8 cm cube there, resting on the plane
    /// and attached to an ARAnchor on that plane so it stays put as tracking refines. D-pad DOWN clears all cubes.
    /// Does nothing while the five-button test is on or while the gaze is on a Gazeable (the pinch belongs to them).
    /// Works whether or not the plane patches are visible.
    /// </summary>
    public sealed class PlaceObjectsController : MonoBehaviour
    {
        private const string FiveButtonRootName = "FiveButtonTest Buttons";
        private const float FiveButtonCheckSeconds = 0.5f;

        private GazePointer _gaze;
        private HandTrackingService _hands;
        private ARPlaneManager _planes;
        private ARRaycastManager _raycaster;
        private ARAnchorManager _anchors;
        private GameObject _preview;
        private readonly List<GameObject> _placed = new List<GameObject>();
        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

        private readonly PinchDetector[] _detectors = { new PinchDetector(), new PinchDetector() };
        private int _lastVersion;
        private bool _pinchStarted;

        private bool _fiveButtonOn;
        private float _nextFiveButtonCheck;

        // Latest plane hit under the gaze, refreshed every frame.
        private bool _hasHit;
        private Pose _hitPose;
        private ARPlane _hitPlane;

        private void Update()
        {
            var pad = Gamepad.current;
            if (pad != null && pad.dpad.down.wasPressedThisFrame) ClearAll();

            if (_hands == null) _hands = FindFirstObjectByType<HandTrackingService>();
            _pinchStarted = false;
            FeedPinch();
        }

        private void LateUpdate()
        {
            if (_gaze == null) _gaze = FindFirstObjectByType<GazePointer>();
            if (_gaze == null) { SetPreview(false); return; }
            if (!EnsureAr()) { SetPreview(false); return; }

            if (Time.unscaledTime >= _nextFiveButtonCheck)
            {
                _nextFiveButtonCheck = Time.unscaledTime + FiveButtonCheckSeconds;
                _fiveButtonOn = GameObject.Find(FiveButtonRootName) != null;
            }

            Transform head = _gaze.transform;
            _hasHit = TryRaycast(head, out _hitPose, out _hitPlane);
            UpdatePreview();

            if (PlacementMath.ShouldPlace(_pinchStarted, _hasHit, _fiveButtonOn, _gaze.CurrentTarget != null))
                Place(head.position);
        }

        // The AR rig is built by HeadPoseDriver; add the raycast and anchor managers to it once it exists.
        private bool EnsureAr()
        {
            if (_planes == null) _planes = FindFirstObjectByType<ARPlaneManager>();
            if (_planes == null) return false;
            if (_raycaster == null)
            {
                _raycaster = _planes.GetComponent<ARRaycastManager>();
                if (_raycaster == null) _raycaster = _planes.gameObject.AddComponent<ARRaycastManager>();
            }
            if (_anchors == null)
            {
                _anchors = _planes.GetComponent<ARAnchorManager>();
                if (_anchors == null) _anchors = _planes.gameObject.AddComponent<ARAnchorManager>();
            }
            return _raycaster != null;
        }

        private bool TryRaycast(Transform head, out Pose pose, out ARPlane plane)
        {
            pose = default(Pose);
            plane = null;
            _hits.Clear();
            if (!_raycaster.Raycast(new Ray(head.position, head.forward), _hits, TrackableType.PlaneWithinPolygon)) return false;
            // Hits are sorted nearest first. Skip planes that were merged into a bigger one.
            for (int i = 0; i < _hits.Count; i++)
            {
                ARPlane p = _planes.GetPlane(_hits[i].trackableId);
                if (p != null && p.subsumedBy != null) continue;
                pose = _hits[i].pose;
                plane = p;
                return true;
            }
            return false;
        }

        private void UpdatePreview()
        {
            if (!_hasHit) { SetPreview(false); return; }
            if (_preview == null) _preview = PlacementVisuals.CreatePreview();
            // Pose.up of an ARCore plane hit is the plane normal; nudge 2 mm up to avoid z-fighting with plane patches.
            Vector3 normal = _hitPose.rotation * Vector3.up;
            _preview.transform.position = _hitPose.position + normal * 0.002f;
            _preview.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            SetPreview(true);
        }

        private void SetPreview(bool on)
        {
            if (_preview != null && _preview.activeSelf != on) _preview.SetActive(on);
        }

        private void Place(Vector3 viewer)
        {
            Vector3 normal = _hitPose.rotation * Vector3.up;
            Quaternion rot = PlacementMath.UprightFacingViewer(normal, _hitPose.position, viewer);

            int remove = PlacementMath.OverflowToRemove(_placed.Count, PlacementMath.MaxObjects);
            for (int i = 0; i < remove; i++) DestroyPlaced(_placed[i]);
            if (remove > 0) _placed.RemoveRange(0, remove);

            // The anchor sits on the plane surface at the hit; the cube is its child, half its size up.
            var anchorPose = new Pose(_hitPose.position, rot);
            GameObject root = MakeAnchor(anchorPose);
            var cube = PlacementVisuals.CreateCube();
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = Vector3.up * (PlacementMath.CubeSize * 0.5f);
            cube.transform.localRotation = Quaternion.identity;
            _placed.Add(root);
        }

        // Attach an ARAnchor to the plane that was hit. If ARCore refuses, fall back to a plain world-fixed object.
        private GameObject MakeAnchor(Pose pose)
        {
            ARAnchor anchor = null;
            if (_anchors != null && _hitPlane != null)
            {
                try { anchor = _anchors.AttachAnchor(_hitPlane, pose); }
                catch (System.Exception e) { UnityEngine.Debug.LogWarning("PlaceObjects: AttachAnchor failed: " + e.Message); }
            }
            if (anchor != null) return anchor.gameObject;
            var go = new GameObject("PlacedAnchor (no ARAnchor)");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return go;
        }

        /// <summary>Removes every placed object.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < _placed.Count; i++) DestroyPlaced(_placed[i]);
            _placed.Clear();
        }

        /// <summary>Number of objects currently placed.</summary>
        public int PlacedCount { get { return _placed.Count; } }

        private static void DestroyPlaced(GameObject go)
        {
            if (go != null) Destroy(go);
        }

        // Same pinch feed as the five-button test: one detector per hand, only on a new tracking result.
        private void FeedPinch()
        {
            if (_hands == null) return;
            float now = Time.unscaledTime;
            int version = _hands.ResultVersion;
            if (version == _lastVersion)
            {
                // No new result this app frame: only time out a held pinch. UpdateLost here would cancel the debounce
                // every frame between results, so a pinch could never start (bug in v0.1.92-0.1.114).
                for (int i = 0; i < _detectors.Length; i++) _detectors[i].Tick(now);
                return;
            }
            _lastVersion = version;

            var frame = _hands.LatestFrame;
            float aspect = frame.Intrinsics.IsValid ? frame.Intrinsics.Width / frame.Intrinsics.Height : 1f;
            IReadOnlyList<HandPose> hands = _hands.LatestHands;
            bool[] seen = new bool[_detectors.Length];

            for (int h = 0; h < hands.Count && h < _detectors.Length; h++)
            {
                int slot = SlotFor(hands[h], h, seen);
                Vector3 thumb, index; float scale;
                if (!PinchLandmarks.TryGet(hands[h].Landmarks, aspect, out thumb, out index, out scale)) continue;
                seen[slot] = true;
                var d = _detectors[slot];
                bool was = d.IsPinching;
                d.Update(thumb, index, scale, now);
                if (!was && d.IsPinching) _pinchStarted = true;
            }
            for (int i = 0; i < _detectors.Length; i++)
                if (!seen[i]) _detectors[i].UpdateLost(now);
        }

        private static int SlotFor(HandPose pose, int index, bool[] taken)
        {
            int slot = pose.Side == HandSide.Right ? 1 : pose.Side == HandSide.Left ? 0 : index;
            if (taken[slot]) slot = 1 - slot;
            return slot;
        }

        private void OnDestroy()
        {
            ClearAll();
            if (_preview != null) Destroy(_preview);
        }
    }
}
