using DiyVr.Hands;
using DiyVr.Passthrough;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests.Hands
{
    public class HandPassthroughMapTests
    {
        // Off-centre principal point on purpose (S23 style numbers, scaled to the landmark image).
        private static readonly HandIntrinsics K = new HandIntrinsics(500f, 510f, 330f, 175f, 640f, 360f);
        private const float QuadDistance = 0.12f;

        private static void AssertNear(Vector3 expected, Vector3 actual, float tol = 1e-5f)
        {
            Assert.AreEqual(expected.x, actual.x, tol, "x");
            Assert.AreEqual(expected.y, actual.y, tol, "y");
            Assert.AreEqual(expected.z, actual.z, tol, "z");
        }

        [Test]
        public void PrincipalPointPixel_IsStraightAheadOfTheEye()
        {
            Vector3 p = HandPassthroughMap.PlanePoint(K.Cx / K.Width, K.Cy / K.Height, K, 0.11f);
            AssertNear(new Vector3(0f, 0f, 0.11f), p);
        }

        [Test]
        public void ImageCentre_IsQuadCentre_WithCentredPrincipalPoint()
        {
            var k = new HandIntrinsics(500f, 500f, 320f, 180f, 640f, 360f);
            Vector3[] quad = PassthroughLayout.IntrinsicsQuad(k.Fx, k.Fy, k.Cx, k.Cy, k.Width, k.Height, QuadDistance);
            Vector3 centreOfQuad = (quad[0] + quad[2]) * 0.5f;
            AssertNear(centreOfQuad, HandPassthroughMap.PlanePoint(0.5f, 0.5f, k, QuadDistance));
        }

        [Test]
        public void ImageCorners_AreQuadCorners_MatchingIntrinsicsQuad()
        {
            Vector3[] quad = PassthroughLayout.IntrinsicsQuad(K.Fx, K.Fy, K.Cx, K.Cy, K.Width, K.Height, QuadDistance);
            // IntrinsicsQuad order: bottom-left, bottom-right, top-right, top-left. Image v is down, so v=1 is the bottom.
            AssertNear(quad[0], HandPassthroughMap.PlanePoint(0f, 1f, K, QuadDistance));
            AssertNear(quad[1], HandPassthroughMap.PlanePoint(1f, 1f, K, QuadDistance));
            AssertNear(quad[2], HandPassthroughMap.PlanePoint(1f, 0f, K, QuadDistance));
            AssertNear(quad[3], HandPassthroughMap.PlanePoint(0f, 0f, K, QuadDistance));
        }

        [Test]
        public void AnyPixel_LiesOnTheLineThroughTheQuadPoint_AtAnyDistance()
        {
            // The skeleton plane (0.11 m) and the image quad (0.12 m) must show a pixel in the same direction, so the
            // skeleton sits exactly on the camera image of the hand.
            Vector3 a = HandPassthroughMap.PlanePoint(0.3f, 0.7f, K, QuadDistance);
            Vector3 b = HandPassthroughMap.PlanePoint(0.3f, 0.7f, K, HandPassthroughMap.SkeletonDistance);
            Assert.AreEqual(a.x / a.z, b.x / b.z, 1e-5f);
            Assert.AreEqual(a.y / a.z, b.y / b.z, 1e-5f);
        }

        [Test]
        public void SkeletonPlane_IsInFrontOfTheImageQuad_AndSharedByBothEyes()
        {
            Assert.Less(HandPassthroughMap.SkeletonDistance, QuadDistance);
            // Mono image: the point depends only on the pixel, never on the eye, so both eyes get identical local
            // positions and the skeleton has zero disparity like the image (the bug was real 3D disparity).
            Vector3[] lm = { new Vector3(0.4f, 0.6f, -0.1f) };
            Assert.AreEqual(0.11f, HandPassthroughMap.PlanePoints(lm, K, 0.11f)[0].z, 1e-6f);
        }

        [Test]
        public void ScaledSize_KeepsAngularSizeOfTheOldSkeleton()
        {
            float joint = HandPassthroughMap.ScaledSize(0.008f);
            Assert.AreEqual(0.008f / 0.4f, joint / HandPassthroughMap.SkeletonDistance, 1e-6f);
            Assert.AreEqual(0.008f, HandPassthroughMap.ScaledSize(0.008f, 0.4f), 1e-9f);
        }

        [Test]
        public void Extrapolator_ContinuesTheVelocity_AndStopsAtOneInterval()
        {
            var prev = new Vector3(0f, 0f, 0f);
            var cur = new Vector3(0.1f, 0.2f, 0f);
            AssertNear(cur, HandExtrapolator.Predict(prev, cur, 0.066f, 0f));
            AssertNear(new Vector3(0.15f, 0.3f, 0f), HandExtrapolator.Predict(prev, cur, 0.066f, 0.033f));
            AssertNear(new Vector3(0.2f, 0.4f, 0f), HandExtrapolator.Predict(prev, cur, 0.066f, 0.066f));
            // Longer than one interval: capped, never runs away.
            AssertNear(new Vector3(0.2f, 0.4f, 0f), HandExtrapolator.Predict(prev, cur, 0.066f, 1.0f));
        }

        [Test]
        public void Extrapolator_IgnoresUnusableIntervals()
        {
            var prev = Vector3.zero;
            var cur = Vector3.one;
            AssertNear(cur, HandExtrapolator.Predict(prev, cur, 0f, 0.03f));
            AssertNear(cur, HandExtrapolator.Predict(prev, cur, 5f, 0.03f)); // old sample: hand was lost in between
        }
    }
}
