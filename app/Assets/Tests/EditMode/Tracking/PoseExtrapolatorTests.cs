using DiyVr.Tracking;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class PoseExtrapolatorTests
    {
        [Test]
        public void Position_ConstantVelocityIsExtrapolated()
        {
            var e = new PoseExtrapolator();
            for (int i = 0; i < 6; i++) e.AddSample(i * 0.01, new Vector3(i * 0.01f, 1f, 0f), Quaternion.identity);
            e.Predict(0.02f, out Vector3 p, out Quaternion r);
            // Last sample x = 0.05 m at 1 m/s, +20 ms = 0.07 m.
            Assert.AreEqual(0.07f, p.x, 1e-4f);
            Assert.AreEqual(1f, p.y, 1e-5f);
            Assert.Less(Quaternion.Angle(r, Quaternion.identity), 0.001f);
        }

        [Test]
        public void Rotation_ConstantAngularVelocityIsExtrapolated()
        {
            var e = new PoseExtrapolator();
            // 90 deg/s about Y, 10 ms apart: 0.9 deg per step.
            for (int i = 0; i < 6; i++) e.AddSample(i * 0.01, Vector3.zero, Quaternion.Euler(0f, 0.9f * i, 0f));
            e.Predict(0.02f, out Vector3 p, out Quaternion r);
            // Last = 4.5 deg, +20 ms = +1.8 deg -> 6.3 deg.
            Assert.Less(Quaternion.Angle(r, Quaternion.Euler(0f, 6.3f, 0f)), 0.01f);
        }

        [Test]
        public void Rotation_WorksWhenSamplesFlipQuaternionSign()
        {
            var e = new PoseExtrapolator();
            for (int i = 0; i < 6; i++)
            {
                Quaternion q = Quaternion.Euler(0f, 0.9f * i, 0f);
                if (i % 2 == 1) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                e.AddSample(i * 0.01, Vector3.zero, q);
            }
            e.Predict(0.02f, out Vector3 p, out Quaternion r);
            Assert.Less(Quaternion.Angle(r, Quaternion.Euler(0f, 6.3f, 0f)), 0.05f);
        }

        [Test]
        public void Stationary_PredictsSamePose()
        {
            var e = new PoseExtrapolator();
            Quaternion q = Quaternion.Euler(10f, 20f, 30f);
            for (int i = 0; i < 6; i++) e.AddSample(i * 0.01, new Vector3(1f, 2f, 3f), q);
            e.Predict(0.02f, out Vector3 p, out Quaternion r);
            Assert.AreEqual(0f, (p - new Vector3(1f, 2f, 3f)).magnitude, 1e-5f);
            Assert.Less(Quaternion.Angle(r, q), 0.001f);
        }

        [Test]
        public void Horizon_IsClamped()
        {
            var e = new PoseExtrapolator();
            for (int i = 0; i < 6; i++) e.AddSample(i * 0.01, new Vector3(i * 0.01f, 0f, 0f), Quaternion.identity);
            e.Predict(1.0f, out Vector3 p, out Quaternion r);
            Assert.AreEqual(0.05f + e.MaxHorizon * 1f, p.x, 1e-4f); // 1 m/s * 0.05 s
        }

        [Test]
        public void SingleSample_ReturnsItUnchanged()
        {
            var e = new PoseExtrapolator();
            e.AddSample(1.0, new Vector3(1f, 2f, 3f), Quaternion.identity);
            e.Predict(0.02f, out Vector3 p, out Quaternion r);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), p);
        }

        [Test]
        public void DuplicateOrOldTimestamps_AreIgnored()
        {
            var e = new PoseExtrapolator();
            e.AddSample(1.0, Vector3.zero, Quaternion.identity);
            e.AddSample(1.0, Vector3.one, Quaternion.identity);
            e.AddSample(0.5, Vector3.one, Quaternion.identity);
            Assert.AreEqual(1, e.Count);
        }
    }
}
