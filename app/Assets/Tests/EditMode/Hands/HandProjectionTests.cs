using DiyVr.Hands;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests.Hands
{
    public class HandProjectionTests
    {
        private static readonly HandIntrinsics K = new HandIntrinsics(500f, 500f, 320f, 240f, 640f, 480f);

        private static Vector3[] Hand(float x, float y, float palmNormX = 0.1f)
        {
            var lm = new Vector3[HandPose.LandmarkCount];
            for (int i = 0; i < lm.Length; i++) lm[i] = new Vector3(x, y, 0f);
            lm[HandProjection.MiddleMcp] = new Vector3(x + palmNormX, y, 0f);
            return lm;
        }

        [Test]
        public void CentrePixelIsStraightAhead()
        {
            Vector3 d = HandProjection.Direction(320f, 240f, K);
            Assert.AreEqual(0f, d.x, 1e-6f);
            Assert.AreEqual(0f, d.y, 1e-6f);
            Assert.AreEqual(1f, d.z, 1e-6f);
        }

        [Test]
        public void RightIsPlusXAndUpIsPlusY()
        {
            Vector3 d = HandProjection.Direction(420f, 140f, K);
            Assert.AreEqual(0.2f, d.x, 1e-5f);
            Assert.AreEqual(0.2f, d.y, 1e-5f); // above the centre (smaller v) is +y
        }

        [Test]
        public void KnownPalmSizeGivesExpectedDepth()
        {
            // 64 px palm (0.1 of 640), f 500, 0.085 m palm -> 0.664 m
            float z = HandProjection.EstimateDepth(Hand(0.4f, 0.5f), K);
            Assert.AreEqual(500f * 0.085f / 64f, z, 1e-4f);
        }

        [Test]
        public void DepthIsClamped()
        {
            Assert.AreEqual(HandProjection.MinDepthMetres, HandProjection.DepthFromPalm(500f, 5000f, 0.085f));
            Assert.AreEqual(HandProjection.MaxDepthMetres, HandProjection.DepthFromPalm(500f, 5f, 0.085f));
            Assert.AreEqual(HandProjection.MaxDepthMetres, HandProjection.DepthFromPalm(500f, 0f, 0.085f));
        }

        [Test]
        public void CentredHandProjectsOnTheAxisAtTheGivenDepth()
        {
            Vector3[] p = HandProjection.Project(Hand(0.5f, 0.5f, 0f), K, 0.6f);
            Assert.AreEqual(0f, p[0].x, 1e-5f);
            Assert.AreEqual(0f, p[0].y, 1e-5f);
            Assert.AreEqual(0.6f, p[0].z, 1e-5f);
        }

        [Test]
        public void ReprojectingTheResultGivesTheOriginalPixel()
        {
            Vector3[] p = HandProjection.Project(Hand(0.4f, 0.3f), K, 0.5f);
            float u = K.Cx + K.Fx * p[9].x / p[9].z;
            float v = K.Cy - K.Fy * p[9].y / p[9].z;
            Assert.AreEqual(0.5f * 640f, u, 1e-3f);
            Assert.AreEqual(0.3f * 480f, v, 1e-3f);
        }

        [Test]
        public void NegativeRelativeZIsCloserToTheCamera()
        {
            Vector3[] lm = Hand(0.5f, 0.5f);
            lm[8] = new Vector3(0.5f, 0.5f, -0.1f);
            Vector3[] p = HandProjection.Project(lm, K, 0.5f);
            Assert.Less(p[8].z, p[0].z);
            Assert.AreEqual(0.5f - 0.1f * 640f * 0.5f / 500f, p[8].z, 1e-4f);
        }

        [Test]
        public void FallbackIntrinsicsAreValid()
        {
            Assert.IsTrue(HandIntrinsics.Fallback(640f, 480f).IsValid);
            Assert.IsFalse(default(HandIntrinsics).IsValid);
        }

        [Test]
        public void ToWorldAppliesTheCameraPose()
        {
            var f = new HandFrameInfo { CameraPosition = new Vector3(1f, 2f, 3f), CameraRotation = Quaternion.Euler(0f, 90f, 0f) };
            Vector3 w = f.ToWorld(new Vector3(0f, 0f, 1f)); // forward becomes +x
            Assert.AreEqual(2f, w.x, 1e-5f);
            Assert.AreEqual(2f, w.y, 1e-5f);
            Assert.AreEqual(3f, w.z, 1e-5f);
        }

        [Test]
        public void SkeletonHasTwentyOneValidBones()
        {
            Assert.AreEqual(21, HandSkeleton.BoneCount);
            for (int i = 0; i < HandSkeleton.BoneCount; i++)
                for (int j = 0; j < 2; j++)
                    Assert.That(HandSkeleton.Bones[i, j], Is.InRange(0, 20));
        }

        [Test]
        public void OneEuroConvergesToAConstantInput()
        {
            var f = new HandOneEuro();
            float v = 0f;
            for (int i = 0; i < 60; i++) v = f.Filter(i == 0 ? 0f : 1f, i / 15f);
            Assert.AreEqual(1f, v, 0.02f);
        }

        [Test]
        public void OneEuroFirstSamplePassesThroughAndNoiseIsReduced()
        {
            var f = new HandOneEuro();
            Assert.AreEqual(5f, f.Filter(5f, 0f));
            float maxDev = 0f;
            for (int i = 1; i < 30; i++)
            {
                float v = f.Filter(5f + (i % 2 == 0 ? 0.01f : -0.01f), i / 15f);
                maxDev = Mathf.Max(maxDev, Mathf.Abs(v - 5f));
            }
            Assert.Less(maxDev, 0.01f);
        }

        [Test]
        public void ChaseConvergesAndNeverOvershoots()
        {
            Vector3 p = Vector3.zero, target = new Vector3(1f, 0f, 0f);
            for (int i = 0; i < 120; i++)
            {
                p = HandChase.Step(p, target, 1f / 60f, 0.05f);
                Assert.LessOrEqual(p.x, 1f);
            }
            Assert.AreEqual(1f, p.x, 1e-3f);
        }
    }
}
