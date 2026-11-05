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
        public void LensGrid_IsHidden_WhileCameraViewIsOn_EvenIfToolIsOpen()
        {
            Assert.IsFalse(SceneVisibility.LensGridVisible(true, true));
            Assert.IsFalse(SceneVisibility.LensGridVisible(true, false));
        }

        [Test]
        public void LensGrid_IsShown_WhenToolIsOpenAndCameraViewIsOff()
        {
            Assert.IsTrue(SceneVisibility.LensGridVisible(false, true));
        }

        [Test]
        public void LensGrid_IsHidden_WhenToolIsClosed()
        {
            Assert.IsFalse(SceneVisibility.LensGridVisible(false, false));
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
                SceneVisibility.LensGridVisible(true, true);
                Assert.IsTrue(cube.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(head);
            }
        }
    }
}
