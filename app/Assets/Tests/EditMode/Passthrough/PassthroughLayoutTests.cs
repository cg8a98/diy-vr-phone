using DiyVr.Core;
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

        // 70 degree horizontal FOV on a 640x480 image: fx = 320 / tan(35 deg).
        private static readonly float Fx70 = 320f / Mathf.Tan(35f * Mathf.Deg2Rad);

        [Test]
        public void IntrinsicsQuad_Symmetric_IsSymmetricAndTrueSize()
        {
            Vector3[] c = PassthroughLayout.IntrinsicsQuad(Fx70, Fx70, 320f, 240f, 640f, 480f, 0.12f);
            float halfW = Mathf.Tan(35f * Mathf.Deg2Rad) * 0.12f;
            Assert.AreEqual(-halfW, c[0].x, 1e-5f);
            Assert.AreEqual(halfW, c[1].x, 1e-5f);
            Assert.AreEqual(c[0].x, -c[1].x, 1e-6f);
            Assert.AreEqual(c[0].y, -c[3].y, 1e-6f);
            Assert.AreEqual(halfW * 480f / 640f, c[3].y, 1e-5f); // square pixels: height follows the aspect
            for (int i = 0; i < 4; i++) Assert.AreEqual(0.12f, c[i].z, 1e-6f);
        }

        [Test]
        public void IntrinsicsQuad_OffCentrePrincipalPoint_ShiftsQuadNotOpticalAxis()
        {
            Vector3[] c = PassthroughLayout.IntrinsicsQuad(500f, 500f, 400f, 200f, 640f, 480f, 1f);
            Assert.AreEqual(-400f / 500f, c[0].x, 1e-5f);
            Assert.AreEqual(240f / 500f, c[1].x, 1e-5f);
            Assert.AreEqual(200f / 500f, c[3].y, 1e-5f);   // top is cy above the axis
            Assert.AreEqual(-280f / 500f, c[0].y, 1e-5f);  // bottom is h - cy below it
        }

        [Test]
        public void IntrinsicsQuad_VisibleCrop_NarrowsVerticalOnly()
        {
            Rect crop = PassthroughLayout.CenterCrop(2f, 4f / 3f);
            Vector3[] full = PassthroughLayout.IntrinsicsQuad(Fx70, Fx70, 320f, 240f, 640f, 480f, 1f);
            Vector3[] c = PassthroughLayout.IntrinsicsQuad(Fx70, Fx70, 320f, 240f, 640f, 480f, 1f, crop);
            Assert.AreEqual(full[0].x, c[0].x, 1e-5f);
            Assert.Less(c[3].y, full[3].y);
            Assert.AreEqual(c[1].x - c[0].x, 2f * (c[3].y - c[0].y), 1e-4f); // aspect of the crop is 2
        }

        [Test]
        public void IntrinsicsQuad_SameInputsForBothEyes_AreIdentical()
        {
            Vector3[] l = PassthroughLayout.IntrinsicsQuad(Fx70, Fx70, 322f, 238f, 640f, 480f, 0.12f);
            Vector3[] r = PassthroughLayout.IntrinsicsQuad(Fx70, Fx70, 322f, 238f, 640f, 480f, 0.12f);
            for (int i = 0; i < 4; i++) Assert.AreEqual(l[i], r[i]);
        }

        [Test]
        public void SymmetricQuad_FallbackMatchesFov()
        {
            Vector3[] c = PassthroughLayout.SymmetricQuad(70f, 55f, 0.12f);
            Assert.AreEqual(Mathf.Tan(35f * Mathf.Deg2Rad) * 0.12f, c[1].x, 1e-5f);
            Assert.AreEqual(Mathf.Tan(27.5f * Mathf.Deg2Rad) * 0.12f, c[2].y, 1e-5f);
            Assert.AreEqual(-c[1].x, c[0].x, 1e-6f);
        }

        [Test]
        public void CameraFov_KnownLens_Gives70Degrees()
        {
            Vector2 fov = CameraFov.Degrees(Fx70, Fx70, 320f, 240f, 640f, 480f);
            Assert.AreEqual(70f, fov.x, 1e-3f);
            Assert.AreEqual(2f * Mathf.Atan(240f / Fx70) * Mathf.Rad2Deg, fov.y, 1e-3f);
        }

        [Test]
        public void CameraFov_BadInput_IsZero()
        {
            Assert.AreEqual(Vector2.zero, CameraFov.Degrees(0f, 500f, 1f, 1f, 640f, 480f));
        }
    }
}
