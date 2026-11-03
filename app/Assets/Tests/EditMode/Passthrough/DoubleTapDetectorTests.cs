using DiyVr.Passthrough;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class DoubleTapDetectorTests
    {
        private static readonly Vector2 Centre = new Vector2(1000f, 500f);

        private static bool Tap(DoubleTapDetector d, double t, Vector2 pos, double hold = 0.08)
        {
            d.OnPress(t, pos);
            return d.OnRelease(t + hold, pos);
        }

        [Test]
        public void TwoQuickTaps_Fire()
        {
            var d = new DoubleTapDetector();
            Assert.IsFalse(Tap(d, 0.0, Centre));
            Assert.IsTrue(Tap(d, 0.25, Centre));
        }

        [Test]
        public void SingleTap_DoesNotFire()
        {
            Assert.IsFalse(Tap(new DoubleTapDetector(), 0.0, Centre));
        }

        [Test]
        public void SlowSecondTap_DoesNotFire_ButCountsAsFirstOfNext()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            Assert.IsFalse(Tap(d, 1.0, Centre));
            Assert.IsTrue(Tap(d, 1.2, Centre));
        }

        [Test]
        public void LongPress_IsNotATap()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            Assert.IsFalse(Tap(d, 0.2, Centre, 0.6));
        }

        [Test]
        public void Drag_IsNotATap()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            d.OnPress(0.2, Centre);
            Assert.IsFalse(d.OnRelease(0.25, Centre + new Vector2(200f, 0f)));
        }

        [Test]
        public void TapsFarApart_DoNotFire()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            Assert.IsFalse(Tap(d, 0.2, Centre + new Vector2(600f, 0f)));
        }

        [Test]
        public void DoubleTapInTopLeftCorner_Fires()
        {
            // The old overlay-corner exclusion is gone: a double tap works anywhere.
            var d = new DoubleTapDetector();
            var corner = new Vector2(50f, 1030f);
            Assert.IsFalse(Tap(d, 0.0, corner));
            Assert.IsTrue(Tap(d, 0.2, corner));
        }

        [Test]
        public void ThirdTap_StartsNewSequence()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            Assert.IsTrue(Tap(d, 0.2, Centre));
            Assert.IsFalse(Tap(d, 0.4, Centre));
        }

        [Test]
        public void Cancel_ForgetsFirstTap()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            d.Cancel();
            Assert.IsFalse(Tap(d, 0.2, Centre));
        }
    }
}
