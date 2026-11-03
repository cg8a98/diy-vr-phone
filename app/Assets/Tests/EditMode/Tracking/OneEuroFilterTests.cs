using DiyVr.Tracking;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class OneEuroFilterTests
    {
        private const float Dt = 1f / 60f;

        private static float StdDev(System.Collections.Generic.List<float> v)
        {
            float mean = 0f;
            foreach (var x in v) mean += x;
            mean /= v.Count;
            float s = 0f;
            foreach (var x in v) s += (x - mean) * (x - mean);
            return Mathf.Sqrt(s / v.Count);
        }

        [Test]
        public void Scalar_StationaryNoiseIsReduced()
        {
            var f = new OneEuroFilter(1f, 0f, 1f);
            var rng = new System.Random(1234);
            var input = new System.Collections.Generic.List<float>();
            var output = new System.Collections.Generic.List<float>();
            for (int i = 0; i < 400; i++)
            {
                float noisy = 5f + ((float)rng.NextDouble() - 0.5f) * 0.01f; // +-5 mm
                float y = f.Filter(noisy, Dt);
                if (i >= 100) { input.Add(noisy); output.Add(y); }
            }
            Assert.Less(StdDev(output), 0.5f * StdDev(input));
        }

        [Test]
        public void Position_StationaryNoiseIsReduced()
        {
            var f = new OneEuroPositionFilter(1.5f, 8f, 1f);
            var rng = new System.Random(99);
            var inX = new System.Collections.Generic.List<float>();
            var outX = new System.Collections.Generic.List<float>();
            for (int i = 0; i < 400; i++)
            {
                var n = new Vector3(
                    ((float)rng.NextDouble() - 0.5f) * 0.004f,
                    ((float)rng.NextDouble() - 0.5f) * 0.004f,
                    ((float)rng.NextDouble() - 0.5f) * 0.004f);
                Vector3 y = f.Filter(new Vector3(1f, 1.6f, 0f) + n, Dt);
                if (i >= 100) { inX.Add(n.x); outX.Add(y.x - 1f); }
            }
            Assert.Less(StdDev(outX), 0.5f * StdDev(inX));
        }

        [Test]
        public void Rotation_StationaryNoiseIsReduced()
        {
            var f = new OneEuroRotationFilter(2f, 4f, 1f);
            var rng = new System.Random(7);
            Quaternion truth = Quaternion.Euler(10f, 30f, 0f);
            float inSum = 0f, outSum = 0f;
            int n = 0;
            for (int i = 0; i < 400; i++)
            {
                var noise = Quaternion.Euler(
                    ((float)rng.NextDouble() - 0.5f) * 0.4f,
                    ((float)rng.NextDouble() - 0.5f) * 0.4f,
                    ((float)rng.NextDouble() - 0.5f) * 0.4f);
                Quaternion noisy = noise * truth;
                Quaternion y = f.Filter(noisy, Dt);
                if (i >= 100)
                {
                    inSum += Quaternion.Angle(noisy, truth);
                    outSum += Quaternion.Angle(y, truth);
                    n++;
                }
            }
            Assert.Less(outSum / n, 0.5f * (inSum / n));
        }

        [Test]
        public void Scalar_StepResponseConvergesWithoutOvershoot()
        {
            var f = new OneEuroFilter(1f, 5f, 1f);
            f.Filter(0f, Dt);
            float y = 0f;
            for (int i = 0; i < 180; i++)
            {
                y = f.Filter(1f, Dt);
                Assert.LessOrEqual(y, 1.0001f);
            }
            Assert.AreEqual(1f, y, 0.01f);
        }

        [Test]
        public void Rotation_StepResponseConverges()
        {
            var f = new OneEuroRotationFilter(2f, 4f, 1f);
            Quaternion target = Quaternion.Euler(0f, 60f, 0f);
            f.Filter(Quaternion.identity, Dt);
            Quaternion y = Quaternion.identity;
            for (int i = 0; i < 180; i++) y = f.Filter(target, Dt);
            Assert.Less(Quaternion.Angle(y, target), 0.1f);
        }

        [Test]
        public void Position_HigherBetaReducesLagOnRealMotion()
        {
            float lagLow = RampLag(0f), lagHigh = RampLag(20f);
            Assert.Less(lagHigh, lagLow);
        }

        private static float RampLag(float beta)
        {
            var f = new OneEuroPositionFilter(1f, beta, 1f);
            Vector3 y = Vector3.zero, x = Vector3.zero;
            for (int i = 0; i < 120; i++)
            {
                x = new Vector3(i * Dt, 0f, 0f); // 1 m/s
                y = f.Filter(x, Dt);
            }
            return (x - y).magnitude;
        }

        [Test]
        public void Rotation_HandlesQuaternionSignFlip()
        {
            var f = new OneEuroRotationFilter(2f, 0f, 1f);
            Quaternion q = Quaternion.Euler(0f, 45f, 0f);
            Quaternion neg = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            f.Filter(q, Dt);
            Quaternion y = f.Filter(neg, Dt);
            Assert.Less(Quaternion.Angle(y, q), 0.01f);
        }
    }
}
