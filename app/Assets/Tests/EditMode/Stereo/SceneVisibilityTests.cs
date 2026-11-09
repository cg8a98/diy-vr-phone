using DiyVr.Stereo;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class SceneVisibilityTests
    {
        [Test]
        public void TestGrid_IsHidden_WhileCameraViewIsOn()
        {
            Assert.IsFalse(SceneVisibility.TestGridVisible(true));
        }

        [Test]
        public void TestGrid_IsShown_WhileCameraViewIsOff()
        {
            Assert.IsTrue(SceneVisibility.TestGridVisible(false));
        }

        [Test]
        public void Cube_IsUnaffected_ByGridRule()
        {
            // The cube is not governed by SceneVisibility: it is created active and nothing in the rule can hide it.
            var head = new GameObject("Head");
            var cube = StereoTestContent.CreateCube(null, head.transform, 1f, 0.2f);
            try
            {
                SceneVisibility.TestGridVisible(true);
                Assert.IsTrue(cube.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(head);
            }
        }
    }

    public class PanelPlacementTests
    {
        [Test]
        public void SameAngularSize_ScalesWidthWithDistance()
        {
            Assert.AreEqual(1.75f, PanelPlacement.WidthForSameAngularSize(0.7f, 0.8f, 2.0f), 1e-5f);
            Assert.AreEqual(0.7f, PanelPlacement.WidthForSameAngularSize(0.7f, 0.8f, 0.8f), 1e-6f);
        }

        [Test]
        public void SameAngularSize_KeepsTheSameAngle()
        {
            float before = Mathf.Atan(0.7f * 0.5f / 0.8f);
            float after = Mathf.Atan(PanelPlacement.WidthForSameAngularSize(0.7f, 0.8f, 2.0f) * 0.5f / 2.0f);
            Assert.AreEqual(before, after, 1e-5f);
        }

        [Test]
        public void LensPanel_IsNearTheGridDepth()
        {
            Assert.AreEqual(2.0f, PanelPlacement.LensPanelDistance, 1e-6f);
            Assert.LessOrEqual(Mathf.Abs(PanelPlacement.GridDistance - PanelPlacement.LensPanelDistance), 0.5f);
        }
    }
}
