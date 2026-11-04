using DiyVr.Tracking;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class CameraConfigChooserTests
    {
        private static CameraConfigInfo C(int w, int h, int fps) { return new CameraConfigInfo(w, h, fps); }

        [Test]
        public void EmptyOrNull_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, CameraConfigChooser.ChooseIndex(null));
            Assert.AreEqual(-1, CameraConfigChooser.ChooseIndex(new CameraConfigInfo[0]));
        }

        [Test]
        public void HighestFpsWins_EvenAtLowerResolution()
        {
            var list = new[] { C(1920, 1080, 30), C(640, 480, 60), C(3840, 2160, 30) };
            Assert.AreEqual(1, CameraConfigChooser.ChooseIndex(list));
        }

        [Test]
        public void SameFps_PicksClosestTo1080p()
        {
            var list = new[] { C(640, 480, 60), C(3840, 2160, 60), C(1920, 1080, 60), C(1280, 720, 60) };
            Assert.AreEqual(2, CameraConfigChooser.ChooseIndex(list));
        }

        [Test]
        public void SameFps_NoExact1080p_PicksNearestPixelCount()
        {
            var list = new[] { C(640, 480, 30), C(1600, 900, 30), C(3840, 2160, 30) };
            Assert.AreEqual(1, CameraConfigChooser.ChooseIndex(list));
        }

        [Test]
        public void UnknownFps_LosesToKnownFps()
        {
            var list = new[] { C(1920, 1080, 0), C(1280, 720, 30) };
            Assert.AreEqual(1, CameraConfigChooser.ChooseIndex(list));
        }

        [Test]
        public void FullTie_KeepsFirst()
        {
            var list = new[] { C(1920, 1080, 60), C(1920, 1080, 60) };
            Assert.AreEqual(0, CameraConfigChooser.ChooseIndex(list));
        }
    
        [Test]
        public void ShouldSwitch_SameFpsDifferentResolution_False()
        {
            Assert.IsFalse(CameraConfigChooser.ShouldSwitch(C(640, 480, 30), true, C(1920, 1080, 30)));
        }

        [Test]
        public void ShouldSwitch_HigherFps_True()
        {
            Assert.IsTrue(CameraConfigChooser.ShouldSwitch(C(1920, 1080, 30), true, C(640, 480, 60)));
        }

        [Test]
        public void ShouldSwitch_LowerFps_False()
        {
            Assert.IsFalse(CameraConfigChooser.ShouldSwitch(C(1920, 1080, 60), true, C(1920, 1080, 30)));
        }

        [Test]
        public void ShouldSwitch_NoCurrent_False()
        {
            Assert.IsFalse(CameraConfigChooser.ShouldSwitch(default(CameraConfigInfo), false, C(1920, 1080, 60)));
        }
    }
}
