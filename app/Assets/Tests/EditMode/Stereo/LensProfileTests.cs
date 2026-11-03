using DiyVr.Stereo;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class LensProfileTests
    {
        [Test]
        public void Json_RoundTripKeepsAllValues()
        {
            var a = LensProfile.CreateDefault();
            var b = LensProfile.CreateDefault();
            try
            {
                a.ipd = 0.063f;
                a.k1 = 0.3f;
                a.k2 = 0.2f;
                a.screenToLensDistance = 0.045f;
                a.lensToEyeDistance = 0.02f;
                a.maxHalfFovDegrees = 55f;
                b.FromJson(a.ToJson());
                Assert.AreEqual(0.063f, b.ipd, 1e-7f);
                Assert.AreEqual(0.3f, b.k1, 1e-7f);
                Assert.AreEqual(0.2f, b.k2, 1e-7f);
                Assert.AreEqual(0.045f, b.screenToLensDistance, 1e-7f);
                Assert.AreEqual(0.02f, b.lensToEyeDistance, 1e-7f);
                Assert.AreEqual(55f, b.maxHalfFovDegrees, 1e-5f);
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void ToParams_CopiesValues()
        {
            var a = LensProfile.CreateDefault();
            try
            {
                a.maxHalfFovDegrees = 60f;
                var p = a.ToParams();
                Assert.AreEqual(a.ipd, p.Ipd, 1e-7);
                Assert.AreEqual(a.k1, p.K1, 1e-7);
                Assert.AreEqual(a.screenToLensDistance, p.ScreenToLens, 1e-7);
                Assert.AreEqual(60.0 * System.Math.PI / 180.0, p.MaxHalfFovRadians, 1e-6);
            }
            finally { Object.DestroyImmediate(a); }
        }
    }
}
