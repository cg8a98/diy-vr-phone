using DiyVr.Passthrough;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class PassthroughLayoutTests
    {
        [Test]
        public void SymmetricFrustum_QuadMatchesFov()
        {
            var proj = Matrix4x4.Perspective(60f, 1.5f, 0.1f, 100f);
            Vector3[] c = PassthroughLayout.FrustumQuad(proj, 2f);
            float halfH = 2f * Mathf.Tan(30f * Mathf.Deg2Rad);
            Assert.AreEqual(-halfH * 1.5f, c[0].x, 1e-3f);
            Assert.AreEqual(-halfH, c[0].y, 1e-3f);
            Assert.AreEqual(halfH * 1.5f, c[2].x, 1e-3f);
            Assert.AreEqual(halfH, c[2].y, 1e-3f);
            for (int i = 0; i < 4; i++) Assert.AreEqual(2f, c[i].z, 1e-4f);
        }

        [Test]
        public void AsymmetricFrustum_QuadIsOffCentre()
        {
            // Near plane at 1: left edge -0.5, right edge 1.5, so at distance 3 the edges are -1.5 and 4.5.
            var proj = Matrix4x4.Frustum(-0.5f, 1.5f, -1f, 1f, 1f, 50f);
            Vector3[] c = PassthroughLayout.FrustumQuad(proj, 3f);
            Assert.AreEqual(-1.5f, c[0].x, 1e-3f);
            Assert.AreEqual(4.5f, c[1].x, 1e-3f);
            Assert.AreEqual(-3f, c[0].y, 1e-3f);
            Assert.AreEqual(3f, c[3].y, 1e-3f);
        }

        [Test]
        public void CenterCrop_WiderImage_CropsSides()
        {
            Rect r = PassthroughLayout.CenterCrop(1f, 2f);
            Assert.AreEqual(0.5f, r.width, 1e-5f);
            Assert.AreEqual(0.25f, r.x, 1e-5f);
            Assert.AreEqual(1f, r.height, 1e-5f);
        }

        [Test]
        public void CenterCrop_TallerImage_CropsTopBottom()
        {
            Rect r = PassthroughLayout.CenterCrop(2f, 1f);
            Assert.AreEqual(0.5f, r.height, 1e-5f);
            Assert.AreEqual(0.25f, r.y, 1e-5f);
            Assert.AreEqual(1f, r.width, 1e-5f);
        }

        [Test]
        public void CenterCrop_EqualAspect_IsWholeImage()
        {
            Rect r = PassthroughLayout.CenterCrop(1.5f, 1.5f);
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), r);
        }

        [Test]
        public void CenterCrop_BadInput_IsWholeImage()
        {
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), PassthroughLayout.CenterCrop(0f, 1f));
        }
    }
}
