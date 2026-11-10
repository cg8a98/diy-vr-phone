using System.Collections.Generic;
using System.IO;
using DiyVr.Room;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiyVr.Tests
{
    /// <summary>
    /// Runs the real room-frame code that builds scene objects and saves the file (not just the maths): an earlier
    /// ticket shipped scene-building code that threw on the phone because only its maths was tested.
    /// "Restart" here = a new controller reading the same saved file, with the markers reported in a different
    /// world (ARCore starts every session with a new origin and heading).
    /// </summary>
    public class RoomFrameBuildTests
    {
        private const float Mm = 0.001f;

        // The real room, in the first session's world coordinates.
        private static readonly Vector3 MarkerA = new Vector3(0.4f, 1.45f, 2.1f);
        private static readonly Vector3 MarkerB = new Vector3(1.9f, 1.52f, 2.6f);
        private static readonly Vector3 RealSpot = new Vector3(1.1f, 0.74f, 0.9f);

        private readonly List<GameObject> _made = new List<GameObject>();
        private string _path;
        private System.Random _rnd;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Application.temporaryCachePath, "roomframe-build-" + System.Guid.NewGuid().ToString("N") + ".json");
            _rnd = new System.Random(42);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made)
            {
                if (go == null) continue;
                var c = go.GetComponent<RoomFrameController>();
                if (c != null) c.Teardown(); // OnDestroy does not run in EditMode
                Object.DestroyImmediate(go);
            }
            _made.Clear();
            if (File.Exists(_path)) File.Delete(_path);
        }

        private RoomFrameController Start()
        {
            var go = new GameObject("Room Frame (test)");
            _made.Add(go);
            var c = go.AddComponent<RoomFrameController>();
            c.Init(_path);
            return c;
        }

        // Shows both markers to the controller: a full window each, with 1 mm of noise, in the given session's world.
        private void SeeMarkers(RoomFrameController c, Quaternion turn, Vector3 shift, Vector3 markerB, float startTime)
        {
            for (int i = 0; i < RoomFrameSolver.DefaultWindowSize; i++)
                c.FeedMarker(0, turn * MarkerA + shift + Noise(), startTime + i * 0.033f);
            for (int i = 0; i < RoomFrameSolver.DefaultWindowSize; i++)
                c.FeedMarker(1, turn * markerB + shift + Noise(), startTime + 2f + i * 0.033f);
        }

        private Vector3 Noise()
        {
            return new Vector3((float)(_rnd.NextDouble() * 2 - 1), (float)(_rnd.NextDouble() * 2 - 1), (float)(_rnd.NextDouble() * 2 - 1)) * Mm;
        }

        [Test]
        public void Init_BuildsGizmoTestObjectAndStatusText_WithoutErrors()
        {
            RoomFrameController c = Start();

            Assert.IsNotNull(c.Frame, "RoomFrame component");
            Assert.AreSame(c.Frame, RoomFrame.Current, "published frame");
            Assert.IsFalse(c.Frame.IsLocked);
            Assert.IsNotNull(c.Gizmo, "gizmo");
            Assert.IsNotNull(c.TestObject, "test object");
            Assert.IsNotNull(c.MarkerBPoint, "marker B point");
            Assert.IsNotNull(c.Hud, "status text");

            // Three axis bars and the origin cube in the gizmo, one cube each for marker B and the test object.
            Assert.AreEqual(4, c.Gizmo.GetComponentsInChildren<MeshFilter>(true).Length, "gizmo cubes");
            Assert.IsNotNull(c.Gizmo.transform.Find("Axis X"));
            Assert.IsNotNull(c.Gizmo.transform.Find("Axis Y"));
            Assert.IsNotNull(c.Gizmo.transform.Find("Axis Z"));
            Assert.AreEqual(0, c.GetComponentsInChildren<Collider>(true).Length, "display objects must not have colliders");

            var renderers = new List<MeshRenderer>(c.GetComponentsInChildren<MeshRenderer>(true));
            renderers.Add(c.Hud.GetComponent<MeshRenderer>());
            Assert.AreEqual(4 + 2 + 3 + 1, renderers.Count, "cubes + labels + status text");
            foreach (var mr in renderers)
            {
                Assert.IsNotNull(mr, "renderer");
                Assert.IsNotNull(mr.sharedMaterial, "material on " + mr.name);
                Assert.IsNotNull(mr.sharedMaterial.shader, "shader on " + mr.name);
            }
            foreach (var tm in c.GetComponentsInChildren<TextMesh>(true))
                Assert.IsNotNull(tm.font, "font on " + tm.name);

            // Nothing to see in the room until the frame is locked; the status text says what to do.
            Assert.IsFalse(c.Gizmo.activeSelf);
            Assert.IsFalse(c.TestObject.activeSelf);
            Assert.IsTrue(c.Hud.gameObject.activeSelf);
            StringAssert.Contains("not calibrated", c.Hud.text);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void HudAttach_PutsTheTextInFrontOfTheHead()
        {
            RoomFrameController c = Start();
            var head = new GameObject("Head");
            _made.Add(head);
            head.transform.position = new Vector3(1f, 1.6f, -2f);
            RoomFrameVisuals.AttachHud(c.Hud, head.transform);
            Assert.AreSame(head.transform, c.Hud.transform.parent);
            Assert.AreEqual(RoomFrameVisuals.HudDistance, c.Hud.transform.localPosition.z, 1e-5f);
            Assert.Less(c.Hud.transform.localPosition.y, 0f, "below the centre of view");
            c.Hud.transform.SetParent(null, false); // so TearDown removes it with the controller
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FirstStart_SeeingBothMarkers_Calibrates_Locks_AndSaves()
        {
            RoomFrameController c = Start();
            Assert.IsFalse(c.Calibrate(), "cannot calibrate before the markers were seen");
            Assert.IsFalse(c.DropTestObject(RealSpot), "cannot drop a test object before the lock");
            Assert.IsFalse(File.Exists(_path));

            SeeMarkers(c, Quaternion.identity, Vector3.zero, MarkerB, 1f);

            Assert.IsTrue(c.Frame.IsLocked);
            Assert.IsTrue(c.Data.calibrated);
            Assert.IsTrue(File.Exists(_path), "calibration saved");
            Assert.Less(Vector3.Distance(MarkerA, c.transform.position), 1.5f * Mm, "room origin sits on marker A");
            Assert.Less(Vector3.Distance(MarkerB, c.MarkerBPoint.transform.position), 2 * Mm, "marker B point sits on marker B");
            Assert.Less(c.Frame.ResidualMeters, 1 * Mm);
            Assert.Less(c.Frame.SpreadMeters, 2 * Mm);
            Assert.IsTrue(c.Gizmo.activeSelf, "gizmo shown once locked");
            Assert.IsFalse(c.TestObject.activeSelf, "no test object dropped yet");
            StringAssert.Contains("LOCKED", c.StatusText(0f));
            StringAssert.Contains("D-pad RIGHT", c.StatusText(0f));

            Assert.IsTrue(c.DropTestObject(RealSpot));
            Assert.IsTrue(c.TestObject.activeSelf);
            Assert.Less(Vector3.Distance(RealSpot, c.TestObject.transform.position), 1e-4f);
            Assert.IsTrue(RoomFrameData.Load(_path).hasTestObject, "test object saved");

            c.SetShown(false);
            Assert.IsFalse(c.Gizmo.activeSelf);
            Assert.IsFalse(c.TestObject.activeSelf);
            Assert.IsFalse(c.Hud.gameObject.activeSelf);
            c.SetShown(true);
            Assert.IsTrue(c.TestObject.activeSelf);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void FiveRestarts_TestObjectComesBackOnTheSameRealSpot()
        {
            RoomFrameController first = Start();
            SeeMarkers(first, Quaternion.identity, Vector3.zero, MarkerB, 1f);
            Assert.IsTrue(first.DropTestObject(RealSpot));
            first.Teardown();
            Object.DestroyImmediate(first.gameObject);

            for (int restart = 1; restart <= 5; restart++)
            {
                // A new session: ARCore's world starts somewhere else, facing another way.
                Quaternion turn = Quaternion.Euler(0f, 37f * restart + 11f, 0f);
                Vector3 shift = new Vector3(0.8f * restart, -0.05f * restart, -1.3f + restart);

                RoomFrameController c = Start();
                Assert.IsTrue(c.Data.calibrated, "calibration loaded from the file, restart " + restart);
                Assert.IsFalse(c.Frame.IsLocked, "not locked before the markers are seen");
                Assert.IsFalse(c.TestObject.activeSelf);

                SeeMarkers(c, turn, shift, MarkerB, 1f);

                Assert.IsTrue(c.Frame.IsLocked, "locked, restart " + restart);
                Assert.IsTrue(c.TestObject.activeSelf, "test object back, restart " + restart);
                Vector3 expected = turn * RealSpot + shift;
                Assert.Less(Vector3.Distance(expected, c.TestObject.transform.position), 3 * Mm, "test object off its real spot, restart " + restart);
                Assert.Less(Vector3.Distance(expected, c.Frame.RoomToWorld(c.Data.testObject)), 3 * Mm);
                Assert.Less(c.Frame.ResidualMeters, 2 * Mm, "marker check, restart " + restart);

                c.Teardown();
                Object.DestroyImmediate(c.gameObject);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MovedMarker_ShowsInTheMarkerCheck_AndCalibrateAgainClearsIt()
        {
            RoomFrameController first = Start();
            SeeMarkers(first, Quaternion.identity, Vector3.zero, MarkerB, 1f);
            first.Teardown();
            Object.DestroyImmediate(first.gameObject);

            // Marker B was re-stuck 5 cm lower.
            Vector3 movedB = MarkerB + new Vector3(0f, -0.05f, 0f);
            RoomFrameController c = Start();
            SeeMarkers(c, Quaternion.identity, Vector3.zero, movedB, 1f);
            Assert.IsTrue(c.Frame.IsLocked);
            Assert.AreEqual(0.05f, c.Frame.ResidualMeters, 3 * Mm);
            StringAssert.Contains("calibrate again", c.StatusText(0f));

            Assert.IsTrue(c.Calibrate());
            Assert.Less(c.Frame.ResidualMeters, 1e-5f);
            Assert.Less(Vector3.Distance(RoomFrameData.Load(_path).markerBInRoom, c.Data.markerBInRoom), 1e-6f, "new calibration saved");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MarkerPictures_AreInTheApp_Readable_AndDifferent()
        {
            var textures = new Texture2D[RoomMarkers.Count];
            try
            {
                for (int i = 0; i < RoomMarkers.Count; i++)
                {
                    textures[i] = RoomMarkers.LoadTexture(i);
                    Assert.IsNotNull(textures[i], "marker " + RoomMarkers.Letter(i) + " picture missing from Resources");
                    Assert.AreEqual(RoomMarkers.PixelSize, textures[i].width);
                    Assert.AreEqual(RoomMarkers.PixelSize, textures[i].height);
                    Assert.AreEqual(TextureFormat.RGBA32, textures[i].format);
                    Assert.IsTrue(textures[i].isReadable, "ARCore needs a readable texture");
                    Assert.AreEqual(i, RoomMarkers.IndexOf(RoomMarkers.Name(i)));
                }
                Assert.AreEqual(-1, RoomMarkers.IndexOf("something else"));
                Assert.AreEqual(0.15f, RoomMarkers.WidthMeters, 1e-6f, "the print sheet is made for 150 mm markers");

                // High contrast: both black and white present; and the two pictures are not the same.
                Color32[] a = textures[0].GetPixels32();
                Color32[] b = textures[1].GetPixels32();
                int dark = 0, bright = 0, different = 0;
                for (int i = 0; i < a.Length; i += 7)
                {
                    if (a[i].r < 30) dark++;
                    if (a[i].r > 225) bright++;
                    if (a[i].r != b[i].r) different++;
                }
                int sampled = a.Length / 7;
                Assert.Greater(dark, sampled / 10, "marker A has too little black");
                Assert.Greater(bright, sampled / 10, "marker A has too little white");
                Assert.Greater(different, sampled / 4, "markers A and B look alike");
            }
            finally
            {
                foreach (var t in textures) if (t != null) Object.DestroyImmediate(t);
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
