using DiyVr.Stereo;
using NUnit.Framework;

namespace DiyVr.Tests
{
    /// <summary>
    /// Reference values are worked out by hand from r' = r (1 + k1 r^2 + k2 r^4) with the Cardboard v1 lens
    /// (k1 = 0.441, k2 = 0.156).
    /// </summary>
    public class CardboardDistortionTests
    {
        private const double K1 = 0.441, K2 = 0.156;

        [Test]
        public void Distort_AtHalfRadius_IsExactly0p56()
        {
            // r = 0.5: r^2 = 0.25, r^4 = 0.0625.
            // 0.441 * 0.25 = 0.11025 ; 0.156 * 0.0625 = 0.00975 ; sum = 0.12 ; factor = 1.12 ; 0.5 * 1.12 = 0.56
            Assert.AreEqual(0.56, CardboardDistortion.Distort(0.5, K1, K2), 1e-12);
        }

        [Test]
        public void Distort_AtUnitRadius_IsOnePlusK1PlusK2()
        {
            // r = 1: factor = 1 + 0.441 + 0.156 = 1.597.
            Assert.AreEqual(1.597, CardboardDistortion.Distort(1.0, K1, K2), 1e-12);
        }

        [Test]
        public void Distort_AtZero_IsZero()
        {
            Assert.AreEqual(0.0, CardboardDistortion.Distort(0.0, K1, K2), 0.0);
        }

        [Test]
        public void Distort_WithZeroCoefficients_IsIdentity()
        {
            Assert.AreEqual(0.73, CardboardDistortion.Distort(0.73, 0, 0), 1e-12);
        }

        [Test]
        public void DistortionFactor_UsesSquaredRadius()
        {
            // r2 = 0.25 -> 1 + 0.441*0.25 + 0.156*0.0625 = 1.12
            Assert.AreEqual(1.12, CardboardDistortion.DistortionFactor(0.25, K1, K2), 1e-12);
        }

        [Test]
        public void DistortInverse_OfKnownValue_ReturnsHalf()
        {
            Assert.AreEqual(0.5, CardboardDistortion.DistortInverse(0.56, K1, K2), 1e-9);
            Assert.AreEqual(1.0, CardboardDistortion.DistortInverse(1.597, K1, K2), 1e-9);
        }

        [Test]
        public void DistortInverse_OfZero_IsZero()
        {
            Assert.AreEqual(0.0, CardboardDistortion.DistortInverse(0.0, K1, K2), 0.0);
        }

        [Test]
        public void RoundTrip_InverseOfDistort_WithinTolerance()
        {
            for (double r = 0.0; r <= 1.5; r += 0.01)
            {
                double back = CardboardDistortion.DistortInverse(CardboardDistortion.Distort(r, K1, K2), K1, K2);
                Assert.AreEqual(r, back, 1e-4, "r = " + r);
            }
        }

        [Test]
        public void RoundTrip_DistortOfInverse_WithinTolerance()
        {
            for (double s = 0.0; s <= 2.5; s += 0.01)
            {
                double back = CardboardDistortion.Distort(CardboardDistortion.DistortInverse(s, K1, K2), K1, K2);
                Assert.AreEqual(s, back, 1e-4, "s = " + s);
            }
        }

        [Test]
        public void RoundTrip_WorksForOtherLensCoefficients()
        {
            double[][] lenses = { new[] { 0.0, 0.0 }, new[] { 0.34, 0.55 }, new[] { 0.2, 0.0 }, new[] { -0.1, 0.1 } };
            foreach (var k in lenses)
            {
                for (double r = 0.0; r <= 1.2; r += 0.05)
                {
                    double back = CardboardDistortion.DistortInverse(CardboardDistortion.Distort(r, k[0], k[1]), k[0], k[1]);
                    Assert.AreEqual(r, back, 1e-4, "k1 = " + k[0] + " k2 = " + k[1] + " r = " + r);
                }
            }
        }
    }
}
