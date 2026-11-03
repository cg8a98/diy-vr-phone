using DiyVr.Stereo;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class StereoGeometryTests
    {
        // Simple setup, no lens distortion: screen 0.12 x 0.06 m, eyes 0.06 m apart, screen 0.04 m from the lens.
        // Left eye: lens centre at x = -0.03, eye area x from -0.06 to 0 -> -0.03 .. +0.03 from the lens centre.
        // Every edge is 0.03 m from the lens centre, so tan = 0.03 / 0.04 = 0.75 and the half FOV is atan(0.75) = 36.8699 deg.
        private static StereoParams Plain()
        {
            return new StereoParams
            {
                ScreenWidth = 0.12, ScreenHeight = 0.06, Ipd = 0.06, ScreenToLens = 0.04,
                LensCenterYOffset = 0, K1 = 0, K2 = 0, MaxHalfFovRadians = 80 * System.Math.PI / 180.0
            };
        }

        [Test]
        public void NoDistortion_FovIsAtan0p75OnAllSides()
        {
            var g = StereoGeometry.ComputeEye(Plain(), EyeSide.Left);
            Assert.AreEqual(-0.75, g.TanLeft, 1e-9);
            Assert.AreEqual(0.75, g.TanRight, 1e-9);
            Assert.AreEqual(-0.75, g.TanBottom, 1e-9);
            Assert.AreEqual(0.75, g.TanTop, 1e-9);
            Assert.AreEqual(36.8699, g.FovLeftDeg, 1e-3);
            Assert.AreEqual(36.8699, g.FovUpDeg, 1e-3);
        }

        [Test]
        public void WiderIpd_ShrinksOuterFovAndGrowsInnerFov()
        {
            // ipd 0.064, screen width 0.12, d = 0.04. Left lens at x = -0.032.
            // Outer (left) edge: 0.06 - 0.032 = 0.028 m -> tan 0.7. Inner (right) edge: 0.032 m -> tan 0.8.
            var p = Plain();
            p.Ipd = 0.064;
            var l = StereoGeometry.ComputeEye(p, EyeSide.Left);
            var r = StereoGeometry.ComputeEye(p, EyeSide.Right);
            Assert.AreEqual(-0.7, l.TanLeft, 1e-9);
            Assert.AreEqual(0.8, l.TanRight, 1e-9);
            // The right eye is the mirror image.
            Assert.AreEqual(-0.8, r.TanLeft, 1e-9);
            Assert.AreEqual(0.7, r.TanRight, 1e-9);
        }

        [Test]
        public void WithLens_VerticalTanMatchesKnownInverse()
        {
            // Cardboard v1 lens. d = 0.05, screen height 0.056 -> top edge 0.028 m -> s = 0.56.
            // Distort(0.5) = 0.56 (see CardboardDistortionTests) so the visible tan is exactly 0.5.
            var p = Plain();
            p.ScreenToLens = 0.05;
            p.ScreenHeight = 0.056;
            p.K1 = 0.441;
            p.K2 = 0.156;
            var g = StereoGeometry.ComputeEye(p, EyeSide.Left);
            Assert.AreEqual(0.5, g.TanTop, 1e-9);
            Assert.AreEqual(-0.5, g.TanBottom, 1e-9);
            // Horizontal edges: 0.03 / 0.05 = 0.6 -> tan is the r with Distort(r) = 0.6.
            Assert.AreEqual(0.6, CardboardDistortion.Distort(g.TanRight, p.K1, p.K2), 1e-9);
            Assert.Less(g.TanRight, 0.6); // the lens magnifies, so less than the plain angle is needed
        }

        [Test]
        public void FovIsClampedToMaximum()
        {
            var p = Plain();
            p.MaxHalfFovRadians = 30 * System.Math.PI / 180.0; // tan 30 deg = 0.57735
            var g = StereoGeometry.ComputeEye(p, EyeSide.Left);
            Assert.AreEqual(0.57735, g.TanRight, 1e-5);
            Assert.AreEqual(30.0, g.FovRightDeg, 1e-6);
        }

        [Test]
        public void LensCentreMapsToMiddleOfTexture_WhenFrustumIsSymmetric()
        {
            var g = StereoGeometry.ComputeEye(Plain(), EyeSide.Left);
            var uv = StereoGeometry.ScreenToTextureUV(g, 0, 0);
            Assert.AreEqual(0.5f, uv.x, 1e-6f);
            Assert.AreEqual(0.5f, uv.y, 1e-6f);
        }

        [Test]
        public void ScreenCornersMapToTextureCorners_WithoutDistortion()
        {
            var g = StereoGeometry.ComputeEye(Plain(), EyeSide.Left);
            var bl = StereoGeometry.ScreenToTextureUV(g, g.RelLeft, g.RelBottom);
            var tr = StereoGeometry.ScreenToTextureUV(g, g.RelRight, g.RelTop);
            Assert.AreEqual(0f, bl.x, 1e-5f);
            Assert.AreEqual(0f, bl.y, 1e-5f);
            Assert.AreEqual(1f, tr.x, 1e-5f);
            Assert.AreEqual(1f, tr.y, 1e-5f);
        }

        [Test]
        public void EdgeMidpointsMapToTextureEdges_WithLens()
        {
            // On the axes the frustum edge is exactly the undistorted edge, so u or v must be 0 or 1.
            var p = Plain();
            p.K1 = 0.441;
            p.K2 = 0.156;
            var g = StereoGeometry.ComputeEye(p, EyeSide.Right);
            Assert.AreEqual(0f, StereoGeometry.ScreenToTextureUV(g, g.RelLeft, 0).x, 1e-5f);
            Assert.AreEqual(1f, StereoGeometry.ScreenToTextureUV(g, g.RelRight, 0).x, 1e-5f);
            Assert.AreEqual(0f, StereoGeometry.ScreenToTextureUV(g, 0, g.RelBottom).y, 1e-5f);
            Assert.AreEqual(1f, StereoGeometry.ScreenToTextureUV(g, 0, g.RelTop).y, 1e-5f);
        }

        [Test]
        public void Projection_IsAsymmetricAndMatchesFrustum()
        {
            var p = Plain();
            p.Ipd = 0.064;
            var g = StereoGeometry.ComputeEye(p, EyeSide.Left);
            var m = StereoGeometry.Projection(g, 0.1f, 100f);
            // For a frustum, m[0,0] = 2n/(r-l) = 2/(0.8+0.7) and m[0,2] = (r+l)/(r-l) = 0.1/1.5
            Assert.AreEqual(2f / 1.5f, m[0, 0], 1e-5f);
            Assert.AreEqual(0.1f / 1.5f, m[0, 2], 1e-5f);
        }

        [Test]
        public void DistortionMesh_CoversTheLeftHalfOfTheScreen()
        {
            var g = StereoGeometry.ComputeEye(Plain(), EyeSide.Left);
            float aspect = 2f;
            var mesh = StereoGeometry.BuildDistortionMesh(g, aspect, 9);
            try
            {
                Assert.AreEqual(81, mesh.vertexCount);
                Assert.AreEqual(8 * 8 * 6, mesh.triangles.Length);
                var b = mesh.bounds;
                Assert.AreEqual(-aspect, b.min.x, 1e-5f);
                Assert.AreEqual(0f, b.max.x, 1e-5f);
                Assert.AreEqual(-1f, b.min.y, 1e-5f);
                Assert.AreEqual(1f, b.max.y, 1e-5f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void DefaultProfile_HasIpd66p5mm()
        {
            var profile = LensProfile.CreateDefault();
            try { Assert.AreEqual(0.0665f, profile.ipd, 1e-7f); }
            finally { Object.DestroyImmediate(profile); }
        }
    }
}
