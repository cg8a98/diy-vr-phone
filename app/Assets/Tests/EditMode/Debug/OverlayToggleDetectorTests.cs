using DiyVr.Diagnostics;
using NUnit.Framework;

namespace DiyVr.Tests.Diagnostics
{
    public class OverlayToggleDetectorTests
    {
        [Test]
        public void ThreeFingerTap_Toggles()
        {
            var d = new OverlayToggleDetector();
            Assert.IsFalse(d.Update(0.00f, 1));
            Assert.IsFalse(d.Update(0.02f, 3));
            Assert.IsTrue(d.Update(0.10f, 0));
        }

        [Test]
        public void ThreeFingerHold_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0f, 3);
            Assert.IsFalse(d.Update(1f, 0));
        }

        [Test]
        public void TwoFingerTap_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0f, 2);
            Assert.IsFalse(d.Update(0.1f, 0));
        }

        [Test]
        public void SingleTap_DoesNotToggle()
        {
            var d = new OverlayToggleDetector();
            d.Update(0f, 1);
            Assert.IsFalse(d.Update(0.05f, 0));
        }

        [Test]
        public void DoubleTap_DoesNotToggle()
        {
            // The old corner double tap is gone: a double tap belongs to passthrough.
            var d = new OverlayToggleDetector();
            d.Update(0.00f, 1);
            d.Update(0.05f, 0);
            d.Update(0.20f, 1);
            Assert.IsFalse(d.Update(0.25f, 0));
        }

        [Test]
        public void SecondThreeFingerTap_TogglesAgain()
        {
            var d = new OverlayToggleDetector();
            d.Update(0.0f, 3);
            Assert.IsTrue(d.Update(0.1f, 0));
            d.Update(1.0f, 3);
            Assert.IsTrue(d.Update(1.1f, 0));
        }
    }
}
