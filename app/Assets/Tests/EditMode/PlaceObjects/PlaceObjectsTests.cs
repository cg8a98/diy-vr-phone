using DiyVr.Experiments.PlaceObjects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiyVr.Tests
{
    public class PlacementMathTests
    {
        [Test]
        public void ObjectCentre_IsHitPlusNormalTimesHalfSize()
        {
            Vector3 c = PlacementMath.ObjectCentre(new Vector3(1f, 0.75f, 2f), Vector3.up, 0.08f);
            Assert.AreEqual(1f, c.x, 1e-5f);
            Assert.AreEqual(0.79f, c.y, 1e-5f);
            Assert.AreEqual(2f, c.z, 1e-5f);
        }

        [Test]
        public void ObjectCentre_NormalisesTheNormal()
        {
            Vector3 c = PlacementMath.ObjectCentre(Vector3.zero, new Vector3(0f, 5f, 0f), 0.1f);
            Assert.AreEqual(0.05f, c.y, 1e-5f);
        }

        [Test]
        public void Rotation_IsUpright_AndFacesViewer()
        {
            Quaternion q = PlacementMath.UprightFacingViewer(Vector3.up, new Vector3(0f, 0.7f, 0f), new Vector3(3f, 1.6f, 0f));
            Assert.AreEqual(0f, (q * Vector3.up - Vector3.up).magnitude, 1e-4f);
            Vector3 fwd = q * Vector3.forward;
            Assert.AreEqual(1f, fwd.x, 1e-4f, "forward points toward the viewer on the plane");
            Assert.AreEqual(0f, fwd.y, 1e-4f);
        }

        [Test]
        public void Rotation_ViewerDirectlyAbove_StillValid()
        {
            Quaternion q = PlacementMath.UprightFacingViewer(Vector3.up, Vector3.zero, new Vector3(0f, 1.5f, 0f));
            Assert.IsFalse(float.IsNaN(q.x));
            Assert.AreEqual(0f, (q * Vector3.up - Vector3.up).magnitude, 1e-4f);
        }

        [Test]
        public void ShouldPlace_Gating()
        {
            Assert.IsTrue(PlacementMath.ShouldPlace(true, true, false, false));
            Assert.IsFalse(PlacementMath.ShouldPlace(false, true, false, false), "no pinch");
            Assert.IsFalse(PlacementMath.ShouldPlace(true, false, false, false), "no plane");
            Assert.IsFalse(PlacementMath.ShouldPlace(true, true, true, false), "five-button test on");
            Assert.IsFalse(PlacementMath.ShouldPlace(true, true, false, true), "gaze on a Gazeable");
        }

        [Test]
        public void Overflow_RemovesOldestOnlyWhenFull()
        {
            Assert.AreEqual(0, PlacementMath.OverflowToRemove(0, 20));
            Assert.AreEqual(0, PlacementMath.OverflowToRemove(19, 20));
            Assert.AreEqual(1, PlacementMath.OverflowToRemove(20, 20));
            Assert.AreEqual(3, PlacementMath.OverflowToRemove(22, 20));
        }
    }

    /// <summary>Runs the real object-building code, not just the maths (a build-code bug once shipped untested).</summary>
    public class PlacementVisualsBuildTests
    {
        private GameObject _preview;
        private GameObject _cube;

        [TearDown]
        public void TearDown()
        {
            if (_preview != null) Object.DestroyImmediate(_preview);
            if (_cube != null) Object.DestroyImmediate(_cube);
        }

        [Test]
        public void CreatePreview_And_CreateCube_HaveRenderersAndMaterials_WithoutErrors()
        {
            _preview = PlacementVisuals.CreatePreview();
            _cube = PlacementVisuals.CreateCube();

            foreach (var go in new[] { _preview, _cube })
            {
                var mr = go.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr, go.name + " renderer");
                Assert.IsNotNull(mr.sharedMaterial, go.name + " material");
                Assert.IsNull(go.GetComponent<Collider>(), go.name + " must not have a collider");
                Assert.AreEqual(0, go.layer);
            }
            Assert.AreEqual(PlacementMath.CubeSize, _cube.transform.localScale.x, 1e-6f);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
