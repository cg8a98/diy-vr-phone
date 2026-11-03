using DiyVr.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests.Diagnostics
{
    public class OverlayToggleDetectorTests
    {
        private static readonly Vector2 ScreenSize = new Vector2(2316, 1080);
        private static readonly Vector2 Corner = new Vector2(50, 1040);
        private static readonly Vector2 Middle = new Vector2(1000, 500);

        [Test]
        public void ThreeFingerTap_Toggles()
        {
            var d = new OverlayToggleDetector();
            Assert.IsFalse(d.Update(0.00f, 1, Middle, ScreenSize));
            Assert.IsFalse(d.Update(0.02f, 3, Middle, ScreenSize));
            Assert.IsTrue(d.Update(0.10f, 0, Middle, ScreenSize));
        }

        [Test]
        public void ThreeFingerHold_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0f, 3, Middle, ScreenSize);
            Assert.IsFalse(d.Update(1f, 0, Middle, ScreenSize));
        }

        [Test]
        public void DoubleTapInCorner_Toggles()
        {
            var d = new OverlayToggleDetector();
            d.Update(0.00f, 1, Corner, ScreenSize);
            Assert.IsFalse(d.Update(0.05f, 0, Corner, ScreenSize));
            d.Update(0.20f, 1, Corner, ScreenSize);
            Assert.IsTrue(d.Update(0.25f, 0, Corner, ScreenSize));
        }

        [Test]
        public void SingleTapInCorner_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0f, 1, Corner, ScreenSize);
            Assert.IsFalse(d.Update(0.05f, 0, Corner, ScreenSize));
        }

        [Test]
        public void DoubleTapElsewhere_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0.00f, 1, Middle, ScreenSize);
            d.Update(0.05f, 0, Middle, ScreenSize);
            d.Update(0.20f, 1, Middle, ScreenSize);
            Assert.IsFalse(d.Update(0.25f, 0, Middle, ScreenSize));
        }

        [Test]
        public void SlowSecondTap_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0.0f, 1, Corner, ScreenSize);
            d.Update(0.05f, 0, Corner, ScreenSize);
            d.Update(2.0f, 1, Corner, ScreenSize);
            Assert.IsFalse(d.Update(2.05f, 0, Corner, ScreenSize));
        }

        [Test]
        public void InCorner_UsesTopLeftWithBottomLeftOrigin()
        {
            Assert.IsTrue(OverlayToggleDetector.InCorner(new Vector2(10, 1070), ScreenSize));
            Assert.IsFalse(OverlayToggleDetector.InCorner(new Vector2(10, 10), ScreenSize));
            Assert.IsFalse(OverlayToggleDetector.InCorner(new Vector2(2000, 1070), ScreenSize));
        }
    }
}
