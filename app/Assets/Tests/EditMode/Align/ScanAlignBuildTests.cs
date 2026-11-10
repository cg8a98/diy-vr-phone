using System.Collections.Generic;
using System.IO;
using DiyVr.Core;
using DiyVr.Room;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiyVr.Tests
{
    /// <summary>
    /// Runs the real align code that touches scene objects, materials and the saved file (not just the maths).
    /// "Restart" here = a new controller, a new room frame and a freshly loaded scan reading the same saved file,
    /// with the markers reported in a different world (ARCore starts every session with a new origin and heading).
    /// </summary>
    public class ScanAlignBuildTests
    {
        private const string ScanName = "living-room";
        private static readonly RoomPose FirstFrame = new RoomPose(new Vector3(0.4f, 1.45f, 2.1f), 20f);
        private const float FloorInRoom = -1.4f;
        /// <summary>A spot in the scan (say a door-frame corner), in the scan's own coordinates.</summary>
        private static readonly Vector3 ScanSpot = new Vector3(2.5f, 2.0f, -1.5f);

        private readonly List<Object> _made = new List<Object>();
        private readonly List<ScanAlignController> _controllers = new List<ScanAlignController>();
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Application.temporaryCachePath, "scan-align-build-" + System.Guid.NewGuid().ToString("N") + ".json");
            ControllerModal.IsOpen = false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScanAlignController c in _controllers)
                if (c != null) c.Teardown(); // OnDestroy does not run in EditMode
            _controllers.Clear();
            foreach (Object o in _made)
                if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
            ControllerModal.IsOpen = false;
            if (File.Exists(_path)) File.Delete(_path);
        }

        private T Keep<T>(T o) where T : Object
        {
            _made.Add(o);
            return o;
        }

        private ScanAlignController StartController(Transform head)
        {
            var go = Keep(new GameObject("Scan Align (test)"));
            var c = go.AddComponent<ScanAlignController>();
            _controllers.Add(c);
            c.Init(_path);
            c.SetHead(head);
            return c;
        }

        private RoomFrame MakeFrame()
        {
            var go = Keep(new GameObject("Room Frame (test)"));
            var frame = go.AddComponent<RoomFrame>();
            frame.Register();
            return frame;
        }

        private static void Lock(RoomFrame frame, RoomPose pose)
        {
            frame.SetLock(pose, 0.004f, 0.001f, true, FloorInRoom);
        }

        private Transform MakeHead(Vector3 position, float yaw)
        {
            var go = Keep(new GameObject("Head (test)"));
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return go.transform;
        }

        // A stand-in for a loaded scan: root "Scan" at the origin with one textured mesh child, built the way
        // ScanSceneBuilder leaves it (our unlit shader, a base colour texture).
        private GameObject MakeScan(out MeshRenderer renderer, out Material material)
        {
            var root = Keep(new GameObject("Scan"));
            var child = new GameObject("mesh");
            child.transform.SetParent(root.transform, false);

            var mesh = Keep(new Mesh());
            mesh.vertices = new[]
            {
                new Vector3(-1f, 0.2f, -2f), new Vector3(3f, 0.2f, -2f), new Vector3(3f, 2.7f, 4f), new Vector3(-1f, 2.7f, 4f),
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            Shader shader = Shader.Find("DiyVr/StereoUnlit");
            Assert.IsNotNull(shader, "the scan shader is in the project");
            material = Keep(new Material(shader));
            material.mainTexture = Keep(new Texture2D(4, 4));
            renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return root;
        }

        private GameObject MakeScan()
        {
            MeshRenderer renderer;
            Material material;
            return MakeScan(out renderer, out material);
        }

        [Test]
        public void AlignMode_MakesTheScanSeeThrough_ShowsTheLabel_AndTakesTheController()
        {
            RoomFrame frame = MakeFrame();
            Lock(frame, FirstFrame);
            Transform head = MakeHead(new Vector3(1f, 1.6f, 0f), 30f);
            ScanAlignController c = StartController(head);
            MeshRenderer renderer;
            Material original;
            GameObject scan = MakeScan(out renderer, out original);
            c.Bind(scan, ScanName);
            c.Place();

            Assert.IsTrue(c.EnterAlign());
            Assert.IsTrue(c.IsAligning);
            Assert.IsTrue(ControllerModal.IsOpen, "the other controller features are switched off");

            Material ghost = renderer.sharedMaterial;
            Assert.AreNotSame(original, ghost);
            Assert.IsNotNull(ghost.shader);
            Assert.AreEqual(ScanGhost.ShaderName, ghost.shader.name);
            Assert.AreEqual(ScanGhost.Alpha, ghost.GetColor("_Color").a, 1e-4f, "see-through");
            Assert.AreSame(original.mainTexture, ghost.GetTexture("_MainTex"), "still shows the scan's picture");
            Assert.AreEqual(1, c.Ghost.Materials.Count);

            c.RefreshLabel(0f);
            Assert.IsNotNull(c.Label, "label");
            Assert.AreSame(head, c.Label.transform.parent);
            Assert.AreEqual(ScanAlignVisuals.LabelName, c.Label.name);
            Assert.IsNotNull(c.Label.font);
            var labelRenderer = c.Label.GetComponent<MeshRenderer>();
            Assert.IsNotNull(labelRenderer, "TextMesh brings its own renderer");
            Assert.IsNotNull(labelRenderer.sharedMaterial);
            Assert.IsTrue(c.Label.gameObject.activeSelf);
            StringAssert.Contains("Align: not saved, frame locked yes, marker residual 0.4 cm", c.Label.text);
            StringAssert.Contains("A save, B cancel", c.Label.text);

            c.CancelAlign();
            Assert.IsFalse(c.IsAligning);
            Assert.IsFalse(ControllerModal.IsOpen);
            Assert.AreSame(original, renderer.sharedMaterial, "the scan's own material is back");
            Assert.AreEqual(0, c.Ghost.Materials.Count);
            Assert.IsTrue(ghost == null, "see-through material freed");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AlignMode_NeedsAScan_AndALockedFrame()
        {
            RoomFrame frame = MakeFrame(); // not locked
            Transform head = MakeHead(Vector3.up * 1.6f, 0f);
            ScanAlignController c = StartController(head);

            Assert.IsFalse(c.EnterAlign(), "no scan");
            c.RefreshLabel(0f);
            StringAssert.Contains("no scan loaded", c.Status);

            GameObject scan = MakeScan();
            c.Bind(scan, ScanName);
            Assert.IsFalse(c.EnterAlign(), "frame not locked");
            c.RefreshLabel(0f);
            StringAssert.Contains("markers", c.Status);
            Assert.IsFalse(ControllerModal.IsOpen);

            Lock(frame, FirstFrame);
            scan.SetActive(false);
            Assert.IsFalse(c.EnterAlign(), "scan hidden");
            scan.SetActive(true);
            Assert.IsTrue(c.EnterAlign());
            c.CancelAlign();
            Assert.IsFalse(File.Exists(_path));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NeverAligned_StartsInFrontOfYou_OnTheFloor()
        {
            RoomFrame frame = MakeFrame();
            Transform head = MakeHead(new Vector3(2f, 1.6f, 3f), 90f);
            ScanAlignController c = StartController(head);
            GameObject scan = MakeScan();
            c.Bind(scan, ScanName);

            // Frame not locked: the floor is guessed from the eye height.
            c.Place();
            Assert.IsFalse(c.HasSaved);
            Bounds b = ScanAlignVisuals.LocalBounds(scan);
            Assert.AreEqual(0.2f, b.min.y, 1e-5f);
            Assert.AreEqual(1f, b.center.x, 1e-5f);
            Assert.AreEqual(1f, b.center.z, 1e-5f);
            Vector3 middle = scan.transform.TransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            Assert.Less(Vector3.Distance(new Vector3(2f + ScanAlignMath.GuessDistance, 1.6f - ScanAlignMath.GuessEyeHeight, 3f), middle), 1e-4f);

            // Moving the head later does not drag the scan along: the guess is made once.
            head.position = new Vector3(-5f, 1.6f, -5f);
            c.Place();
            Assert.Less(Vector3.Distance(new Vector3(2f + ScanAlignMath.GuessDistance, 1.6f - ScanAlignMath.GuessEyeHeight, 3f),
                scan.transform.TransformPoint(new Vector3(b.center.x, b.min.y, b.center.z))), 1e-4f);

            // A newly loaded scan with the frame locked: the floor comes from the room frame.
            Lock(frame, FirstFrame);
            GameObject second = MakeScan();
            c.Bind(second, "another-room");
            c.Place();
            float floorWorld = FirstFrame.Origin.y + FloorInRoom;
            Assert.AreEqual(floorWorld, second.transform.TransformPoint(new Vector3(b.center.x, b.min.y, b.center.z)).y, 1e-4f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Cancel_PutsTheScanBack_AndSavesNothing()
        {
            RoomFrame frame = MakeFrame();
            Lock(frame, FirstFrame);
            Transform head = MakeHead(new Vector3(1f, 1.6f, 0f), 30f);
            ScanAlignController c = StartController(head);
            GameObject scan = MakeScan();
            c.Bind(scan, ScanName);
            c.Place();
            Vector3 positionBefore = scan.transform.position;
            Quaternion rotationBefore = scan.transform.rotation;

            Assert.IsTrue(c.EnterAlign());
            c.StepAlign(new AlignInput { Move = new Vector2(1f, 1f), Turn = -1f, Lift = 1f }, 1.5f);
            Assert.Greater(Vector3.Distance(positionBefore, scan.transform.position), 0.3f, "the sticks moved the scan");
            Assert.Greater(Quaternion.Angle(rotationBefore, scan.transform.rotation), 10f, "the stick turned the scan");

            c.CancelAlign();
            Assert.Less(Vector3.Distance(positionBefore, scan.transform.position), 1e-5f);
            Assert.Less(Quaternion.Angle(rotationBefore, scan.transform.rotation), 1e-2f);
            Assert.IsFalse(c.HasSaved);
            Assert.IsFalse(File.Exists(_path), "nothing written");

            // Outside align mode the sticks do nothing.
            c.StepAlign(new AlignInput { Move = new Vector2(1f, 1f) }, 1f);
            Assert.Less(Vector3.Distance(positionBefore, scan.transform.position), 1e-5f);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AlignedOnce_ThreeRestarts_ScanComesBackOnTheSameRealSpot()
        {
            // First session: align by hand and save.
            RoomFrame frame = MakeFrame();
            Lock(frame, FirstFrame);
            Transform head = MakeHead(new Vector3(1f, 1.6f, 0f), 30f);
            ScanAlignController first = StartController(head);
            MeshRenderer renderer;
            Material original;
            GameObject scan = MakeScan(out renderer, out original);
            first.Bind(scan, ScanName);
            first.Place();
            Assert.IsTrue(first.EnterAlign());
            first.StepAlign(new AlignInput { Move = new Vector2(0.9f, -0.6f) }, 2.0f);
            first.StepAlign(new AlignInput { Turn = 0.8f }, 2.5f);
            first.StepAlign(new AlignInput { Lift = -0.7f, Fine = true }, 1.0f);
            first.StepAlign(new AlignInput { Move = new Vector2(0f, 1f), Turn = -1f, Fine = true }, 0.7f);
            Assert.IsTrue(first.SaveAlign());
            Assert.IsFalse(first.IsAligning);
            Assert.IsTrue(first.HasSaved);
            Assert.IsTrue(File.Exists(_path), "alignment saved");
            Assert.AreSame(original, renderer.sharedMaterial, "solid again after saving");
            StringAssert.Contains("Align: saved", first.StatusText());

            Vector3 realSpot = scan.transform.TransformPoint(ScanSpot); // where the door-frame corner really is
            float realYaw = scan.transform.eulerAngles.y;
            first.Teardown();
            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(scan);
            Object.DestroyImmediate(frame.gameObject);

            for (int restart = 1; restart <= 3; restart++)
            {
                // A new session: ARCore's world starts somewhere else, facing another way.
                float turnYaw = 41f * restart + 7f;
                Quaternion turn = Quaternion.Euler(0f, turnYaw, 0f);
                Vector3 shift = new Vector3(0.9f * restart, -0.04f * restart, 1.7f - restart);

                RoomFrame f = MakeFrame();
                Transform h = MakeHead(new Vector3(restart, 1.55f, -restart), 200f + restart);
                ScanAlignController c = StartController(h);
                GameObject s = MakeScan();
                c.Bind(s, ScanName);
                Assert.IsTrue(c.HasSaved, "alignment loaded from the file, restart " + restart);

                // Markers not seen yet: the scan waits at the starting guess.
                c.Place();
                StringAssert.Contains("frame locked no", c.StatusText());

                // Both markers seen: the frame locks and the scan goes onto the walls, no nudging.
                Lock(f, new RoomPose(turn * FirstFrame.Origin + shift, FirstFrame.YawDegrees + turnYaw));
                c.Place();

                Vector3 expected = turn * realSpot + shift;
                Assert.Less(Vector3.Distance(expected, s.transform.TransformPoint(ScanSpot)), 1e-3f, "scan off its real spot, restart " + restart);
                Assert.AreEqual(0f, Mathf.DeltaAngle(realYaw + turnYaw, s.transform.eulerAngles.y), 0.02f, "scan turned, restart " + restart);
                Assert.Less(Vector3.Angle(Vector3.up, s.transform.up), 0.01f, "scan stays upright");
                StringAssert.Contains("Align: saved, frame locked yes", c.StatusText());

                c.Teardown();
                Object.DestroyImmediate(c.gameObject);
                Object.DestroyImmediate(s);
                Object.DestroyImmediate(f.gameObject);
            }

            // Another scan file has its own (here: no) alignment.
            RoomFrame other = MakeFrame();
            Lock(other, FirstFrame);
            ScanAlignController last = StartController(MakeHead(Vector3.up * 1.6f, 0f));
            last.Bind(MakeScan(), "another-room");
            Assert.IsFalse(last.HasSaved);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ScanReplacedWhileAligning_ClosesAlignMode()
        {
            RoomFrame frame = MakeFrame();
            Lock(frame, FirstFrame);
            ScanAlignController c = StartController(MakeHead(Vector3.up * 1.6f, 0f));
            c.Bind(MakeScan(), ScanName);
            c.Place();
            Assert.IsTrue(c.EnterAlign());

            c.Bind(MakeScan(), "another-room");

            Assert.IsFalse(c.IsAligning);
            Assert.IsFalse(ControllerModal.IsOpen);
            Assert.IsFalse(c.Ghost.IsOn);
            Assert.IsFalse(File.Exists(_path));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
