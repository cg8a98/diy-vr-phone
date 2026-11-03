using DiyVr.Passthrough;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class DoubleTapDetectorTests
    {
        private const int W = 2316, H = 1080;
        private static readonly Vector2 Centre = new Vector2(1000f, 500f);

        private static bool Tap(DoubleTapDetector d, double t, Vector2 pos, double hold = 0.08)
        {
            d.OnPress(t, pos, W, H);
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
            d.OnPress(0.2, Centre, W, H);
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
        public void TapsInOverlayCorner_AreIgnored()
        {
            var d = new DoubleTapDetector();
            var corner = new Vector2(50f, H - 50f);
            Assert.IsFalse(Tap(d, 0.0, corner));
            Assert.IsFalse(Tap(d, 0.2, corner));
        }

        [Test]
        public void CornerTap_BreaksASequence()
        {
            var d = new DoubleTapDetector();
            Tap(d, 0.0, Centre);
            Tap(d, 0.1, new Vector2(50f, H - 50f));
            Assert.IsFalse(Tap(d, 0.2, Centre));
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

        [Test]
        public void OverlayCorner_Geometry()
        {
            // side = min(2316,1080) * 0.25 = 270; top-left means large y (origin bottom-left).
            Assert.IsTrue(DoubleTapDetector.IsInOverlayCorner(new Vector2(10f, H - 10f), W, H, 0.25f));
            Assert.IsFalse(DoubleTapDetector.IsInOverlayCorner(new Vector2(10f, 10f), W, H, 0.25f));
            Assert.IsFalse(DoubleTapDetector.IsInOverlayCorner(new Vector2(W - 10f, H - 10f), W, H, 0.25f));
            Assert.IsFalse(DoubleTapDetector.IsInOverlayCorner(new Vector2(300f, H - 10f), W, H, 0.25f));
        }
    }
}
