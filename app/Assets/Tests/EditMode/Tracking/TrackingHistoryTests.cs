using DiyVr.Tracking;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class TrackingHistoryTests
    {
        private static readonly Quaternion Id = Quaternion.identity;

        [Test]
        public void NoLoss_CountsZero_AndReasonEmpty()
        {
            var h = new TrackingHistory();
            for (int i = 0; i < 10; i++) h.OnFrame(i * 0.1, true, "None", Vector3.zero, Id);
            Assert.AreEqual(0, h.LossCount);
            Assert.IsFalse(h.InLoss);
            Assert.AreEqual(string.Empty, h.LastLossReason);
            Assert.AreEqual(0f, h.MaxJumpCm);
        }

        [Test]
        public void EachLoss_IsCountedOnce_NotPerFrame()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, true, "None", Vector3.zero, Id);
            h.OnFrame(0.1, false, "InsufficientFeatures", Vector3.zero, Id);
            h.OnFrame(0.2, false, "InsufficientFeatures", Vector3.zero, Id);
            h.OnFrame(0.3, true, "None", Vector3.zero, Id);
            h.OnFrame(0.4, false, "ExcessiveMotion", Vector3.zero, Id);
            Assert.AreEqual(2, h.LossCount);
            Assert.IsTrue(h.InLoss);
        }

        [Test]
        public void Reason_IsLatchedAtLossStart()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, true, "None", Vector3.zero, Id);
            h.OnFrame(0.1, false, "InsufficientFeatures", Vector3.zero, Id);
            h.OnFrame(0.2, false, "ExcessiveMotion", Vector3.zero, Id);
            h.OnFrame(0.3, false, "None", Vector3.zero, Id);
            Assert.AreEqual("InsufficientFeatures", h.LastLossReason);
            h.OnFrame(0.4, true, "None", Vector3.zero, Id);
            Assert.AreEqual("InsufficientFeatures", h.LastLossReason, "must survive the regain");
        }

        [Test]
        public void Reason_SkipsLeadingNone_UsesFirstRealReason()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, true, "None", Vector3.zero, Id);
            h.OnFrame(0.1, false, "None", Vector3.zero, Id);
            h.OnFrame(0.2, false, "SessionInitializing", Vector3.zero, Id);
            h.OnFrame(0.3, false, "Relocalizing", Vector3.zero, Id);
            Assert.AreEqual("SessionInitializing", h.LastLossReason);
        }

        [Test]
        public void NextLoss_ReplacesReason()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, true, "None", Vector3.zero, Id);
            h.OnFrame(0.1, false, "InsufficientLight", Vector3.zero, Id);
            h.OnFrame(0.2, true, "None", Vector3.zero, Id);
            h.OnFrame(0.3, false, "ExcessiveMotion", Vector3.zero, Id);
            Assert.AreEqual("ExcessiveMotion", h.LastLossReason);
        }

        [Test]
        public void Duration_IsStartToFirstTrackedFrame()
        {
            var h = new TrackingHistory();
            h.OnFrame(1.0, true, "None", Vector3.zero, Id);
            h.OnFrame(2.0, false, "ExcessiveMotion", Vector3.zero, Id);
            Assert.AreEqual(2.0, h.LastLossStartTime, 1e-9);
            h.OnFrame(2.2, false, "ExcessiveMotion", Vector3.zero, Id);
            Assert.AreEqual(0.2, h.LastLossSeconds, 1e-9, "grows while still lost");
            h.OnFrame(2.4, true, "None", Vector3.zero, Id);
            Assert.AreEqual(0.4, h.LastLossSeconds, 1e-9);
            h.OnFrame(5.0, true, "None", Vector3.zero, Id);
            Assert.AreEqual(0.4, h.LastLossSeconds, 1e-9, "fixed after the regain");
        }

        [Test]
        public void Jump_IsDistanceAndAngleBetweenLastTrackedAndFirstRegained()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, true, "None", new Vector3(1f, 0f, 0f), Id);
            h.OnFrame(0.1, true, "None", new Vector3(1f, 1f, 0f), Quaternion.Euler(0f, 10f, 0f));
            // Pose while lost is garbage and must be ignored.
            h.OnFrame(0.2, false, "InsufficientFeatures", new Vector3(9f, 9f, 9f), Quaternion.Euler(0f, 90f, 0f));
            h.OnFrame(0.3, true, "None", new Vector3(1f, 1.08f, 0f), Quaternion.Euler(0f, 12f, 0f));
            Assert.AreEqual(8f, h.LastJumpCm, 0.01f);
            Assert.AreEqual(2f, h.LastJumpDeg, 0.01f);
        }

        [Test]
        public void MaxJump_KeepsLargest()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, true, "None", Vector3.zero, Id);
            h.OnFrame(0.1, false, "A", Vector3.zero, Id);
            h.OnFrame(0.2, true, "None", new Vector3(0.15f, 0f, 0f), Quaternion.Euler(0f, 5f, 0f));
            h.OnFrame(0.3, false, "B", Vector3.zero, Id);
            h.OnFrame(0.4, true, "None", new Vector3(0.15f, 0.03f, 0f), Quaternion.Euler(0f, 5f, 0f));
            Assert.AreEqual(3f, h.LastJumpCm, 0.01f);
            Assert.AreEqual(15f, h.MaxJumpCm, 0.01f);
            Assert.AreEqual(5f, h.MaxJumpDeg, 0.01f);
        }

        [Test]
        public void LossBeforeAnyTracking_HasNoJump()
        {
            var h = new TrackingHistory();
            h.OnFrame(0.0, false, "SessionInitializing", Vector3.zero, Id);
            h.OnFrame(0.5, true, "None", new Vector3(5f, 5f, 5f), Quaternion.Euler(0f, 90f, 0f));
            Assert.AreEqual(1, h.LossCount);
            Assert.AreEqual(0f, h.LastJumpCm);
            Assert.AreEqual(0f, h.MaxJumpCm);
        }

        [Test]
        public void Drift_MovingAtSixCmPerSec()
        {
            var h = new TrackingHistory();
            for (int i = 0; i <= 180; i++) // 3 s at 60 Hz, 1 mm per frame = 6 cm/s
                h.OnFrame(i / 60.0, true, "None", new Vector3(i * 0.001f, 0f, 0f), Id);
            Assert.AreEqual(6f, h.DriftCmPerSec, 0.05f);
        }

        [Test]
        public void Drift_OnlyLooksAtLastTwoSeconds()
        {
            var h = new TrackingHistory();
            for (int i = 0; i <= 120; i++) // 2 s of motion
                h.OnFrame(i / 60.0, true, "None", new Vector3(i * 0.001f, 0f, 0f), Id);
            Vector3 still = new Vector3(0.12f, 0f, 0f);
            for (int i = 121; i <= 300; i++) // then 3 s of standing still
                h.OnFrame(i / 60.0, true, "None", still, Id);
            Assert.AreEqual(0f, h.DriftCmPerSec, 0.001f);
        }

        [Test]
        public void Drift_ExcludesLossAndRegainJump()
        {
            var h = new TrackingHistory();
            for (int i = 0; i <= 60; i++) h.OnFrame(i / 60.0, true, "None", Vector3.zero, Id);
            h.OnFrame(1.1, false, "ExcessiveMotion", Vector3.zero, Id);
            for (int i = 0; i <= 60; i++) h.OnFrame(1.2 + i / 60.0, true, "None", new Vector3(0.5f, 0f, 0f), Id);
            Assert.AreEqual(0f, h.DriftCmPerSec, 0.001f);
            Assert.AreEqual(50f, h.LastJumpCm, 0.01f);
        }
    }
}
