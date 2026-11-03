using DiyVr.Tracking;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class HeadPoseTrackerTests
    {
        private const double Dt = 1.0 / 60.0;

        [Test]
        public void FirstSample_PassesThroughExactly()
        {
            var t = new HeadPoseTracker();
            var q = Quaternion.Euler(5f, 6f, 7f);
            t.Update(0.0, new Vector3(1f, 2f, 3f), q, true);
            Assert.IsTrue(t.HasPose);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), t.Position);
            Assert.Less(Quaternion.Angle(t.Rotation, q), 1e-3f);
        }

        [Test]
        public void Stationary_OutputStaysAtTheRawPose()
        {
            var t = new HeadPoseTracker();
            var q = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 120; i++) t.Update(i * Dt, new Vector3(0f, 1.6f, 0f), q, true);
            Assert.AreEqual(0f, (t.Position - new Vector3(0f, 1.6f, 0f)).magnitude, 1e-4f);
            Assert.Less(Quaternion.Angle(t.Rotation, q), 0.01f);
        }

        [Test]
        public void TrackingLost_HoldsLastPoseAndRaisesEventOnce()
        {
            var t = new HeadPoseTracker();
            int lost = 0, regained = 0;
            t.TrackingChanged += tracking => { if (tracking) regained++; else lost++; };

            for (int i = 0; i < 30; i++) t.Update(i * Dt, new Vector3(i * 0.01f, 1f, 0f), Quaternion.identity, true);
            Vector3 heldPos = t.Position;
            Quaternion heldRot = t.Rotation;

            for (int i = 30; i < 60; i++)
                t.Update(i * Dt, new Vector3(50f, 50f, 50f), Quaternion.Euler(90f, 0f, 0f), false);

            Assert.IsFalse(t.IsTracking);
            Assert.AreEqual(heldPos, t.Position);
            Assert.AreEqual(heldRot, t.Rotation);
            Assert.AreEqual(1, lost);
            Assert.AreEqual(0, regained);
        }

        [Test]
        public void TrackingRegained_RestartsFiltersWithoutSmoothingAcrossTheJump()
        {
            var t = new HeadPoseTracker();
            int regained = 0;
            t.TrackingChanged += tracking => { if (tracking) regained++; };
            for (int i = 0; i < 30; i++) t.Update(i * Dt, Vector3.zero, Quaternion.identity, true);
            t.Update(30 * Dt, Vector3.zero, Quaternion.identity, false);

            var newPos = new Vector3(3f, 0f, 0f);
            t.Update(31 * Dt, newPos, Quaternion.identity, true);

            Assert.IsTrue(t.IsTracking);
            Assert.AreEqual(1, regained);
            Assert.AreEqual(0f, (t.Position - newPos).magnitude, 1e-5f);
        }

        [Test]
        public void LostBeforeAnyPose_HasNoPose()
        {
            var t = new HeadPoseTracker();
            t.Update(0.0, Vector3.one, Quaternion.identity, false);
            Assert.IsFalse(t.HasPose);
        }

        [Test]
        public void Bypass_ReturnsRawPose()
        {
            var t = new HeadPoseTracker();
            t.Settings.bypass = true;
            t.Update(0.0, Vector3.zero, Quaternion.identity, true);
            t.Update(Dt, new Vector3(0.1f, 0f, 0f), Quaternion.identity, true);
            Assert.AreEqual(0.1f, t.Position.x, 1e-6f);
        }

        [Test]
        public void Prediction_PutsOutputAheadOfRawDuringSteadyMotion()
        {
            var t = new HeadPoseTracker();
            t.Settings.predictionSeconds = 0.020f;
            Vector3 raw = Vector3.zero;
            for (int i = 0; i < 240; i++)
            {
                raw = new Vector3(i * (float)Dt, 0f, 0f); // 1 m/s
                t.Update(i * Dt, raw, Quaternion.identity, true);
            }
            // Smoothing lags a little, prediction leads 20 ms (2 cm); together the output must beat the
            // smoothed-only lag, i.e. be closer to raw than 2 cm either side.
            Assert.Less(Mathf.Abs(t.Position.x - raw.x), 0.02f);
        }
    }
}
