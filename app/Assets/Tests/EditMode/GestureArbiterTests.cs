using DiyVr.Core;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class GestureArbiterTests
    {
        [Test]
        public void ThreeFingerTap_GoesToOverlay()
        {
            Assert.AreEqual(GestureTarget.Overlay, new GestureArbiter().Route(TouchGesture.ThreeFingerTap));
        }

        [Test]
        public void DoubleTap_GoesToPassthrough()
        {
            Assert.AreEqual(GestureTarget.Passthrough, new GestureArbiter().Route(TouchGesture.DoubleTap));
        }

        [Test]
        public void TwoFingerTap_GoesToLensTool()
        {
            Assert.AreEqual(GestureTarget.LensTool, new GestureArbiter().Route(TouchGesture.TwoFingerTap));
        }

        [Test]
        public void NoGesture_GoesNowhere()
        {
            Assert.AreEqual(GestureTarget.None, new GestureArbiter().Route(TouchGesture.None));
            var open = new GestureArbiter { LensToolOpen = true };
            Assert.AreEqual(GestureTarget.None, open.Route(TouchGesture.None));
        }

        [Test]
        public void LensToolOpen_TakesEveryGesture()
        {
            var a = new GestureArbiter { LensToolOpen = true };
            Assert.AreEqual(GestureTarget.LensTool, a.Route(TouchGesture.ThreeFingerTap));
            Assert.AreEqual(GestureTarget.LensTool, a.Route(TouchGesture.DoubleTap));
            Assert.AreEqual(GestureTarget.LensTool, a.Route(TouchGesture.TwoFingerTap));
        }

        [Test]
        public void LensToolOpen_BlocksOverlayAndPassthrough()
        {
            var a = new GestureArbiter { LensToolOpen = true };
            Assert.IsFalse(a.Allows(GestureTarget.Overlay, TouchGesture.ThreeFingerTap));
            Assert.IsFalse(a.Allows(GestureTarget.Passthrough, TouchGesture.DoubleTap));
        }

        [Test]
        public void ClosingLensTool_RestoresOverlayAndPassthrough()
        {
            var a = new GestureArbiter { LensToolOpen = true };
            a.LensToolOpen = false;
            Assert.IsTrue(a.Allows(GestureTarget.Overlay, TouchGesture.ThreeFingerTap));
            Assert.IsTrue(a.Allows(GestureTarget.Passthrough, TouchGesture.DoubleTap));
        }

        [Test]
        public void EachGesture_HasExactlyOneOwner()
        {
            var a = new GestureArbiter();
            foreach (TouchGesture g in new[] { TouchGesture.ThreeFingerTap, TouchGesture.DoubleTap, TouchGesture.TwoFingerTap })
            {
                int owners = 0;
                foreach (GestureTarget t in new[] { GestureTarget.Overlay, GestureTarget.Passthrough, GestureTarget.LensTool })
                    if (a.Allows(t, g)) owners++;
                Assert.AreEqual(1, owners, g.ToString());
            }
        }

        [Test]
        public void Shared_IsOneInstance()
        {
            Assert.AreSame(GestureArbiter.Shared, GestureArbiter.Shared);
        }
    }
}
