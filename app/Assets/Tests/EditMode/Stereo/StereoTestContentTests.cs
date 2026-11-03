using DiyVr.Stereo;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class StereoTestContentTests
    {
        private GameObject _head, _cube;

        [TearDown]
        public void TearDown()
        {
            if (_head != null) Object.DestroyImmediate(_head);
            if (_cube != null) Object.DestroyImmediate(_cube);
        }

        [Test]
        public void Cube_IsNotParentedToHead_SoItStaysFixedInTheWorld()
        {
            _head = new GameObject("Head");
            _cube = StereoTestContent.CreateCube(null, _head.transform, 1f, 0.2f);
            Assert.IsNull(_cube.transform.parent);

            Vector3 before = _cube.transform.position;
            _head.transform.SetPositionAndRotation(new Vector3(3f, 0f, -2f), Quaternion.Euler(0f, 90f, 0f));
            Assert.AreEqual(before.x, _cube.transform.position.x, 1e-5f);
            Assert.AreEqual(before.y, _cube.transform.position.y, 1e-5f);
            Assert.AreEqual(before.z, _cube.transform.position.z, 1e-5f);
        }

        [Test]
        public void Cube_StartsOneMetreAhead()
        {
            _head = new GameObject("Head");
            _head.transform.SetPositionAndRotation(new Vector3(1f, 1.5f, 2f), Quaternion.Euler(0f, 90f, 0f));
            _cube = StereoTestContent.CreateCube(null, _head.transform, 1f, 0.2f);
            Assert.AreEqual(2f, _cube.transform.position.x, 1e-4f);
            Assert.AreEqual(1.5f, _cube.transform.position.y, 1e-4f);
            Assert.AreEqual(2f, _cube.transform.position.z, 1e-4f);
        }

        [Test]
        public void CubeWorldPosition_TiltedHead_StaysAtHeadHeight()
        {
            Vector3 p = StereoTestContent.CubeWorldPosition(new Vector3(0f, 1.2f, 0f), Quaternion.Euler(-45f, 0f, 0f), 1f);
            Assert.AreEqual(1.2f, p.y, 1e-4f);
            Assert.AreEqual(1f, p.z, 1e-4f);
        }

        [Test]
        public void CubeWorldPosition_LookingStraightUp_FallsBackToWorldForward()
        {
            Vector3 p = StereoTestContent.CubeWorldPosition(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), 1f);
            Assert.AreEqual(1f, p.z, 1e-4f);
        }
    }
}
