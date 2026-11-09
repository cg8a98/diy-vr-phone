using DiyVr.Hands;
using NUnit.Framework;

namespace DiyVr.Tests.Hands
{
    public class HandsCoreTests
    {
        [Test]
        public void RollingAverage_EmptyIsZero()
        {
            Assert.AreEqual(0.0, new RollingAverage(4).Average);
        }

        [Test]
        public void RollingAverage_DropsOldestSamples()
        {
            var avg = new RollingAverage(3);
            avg.Add(10); avg.Add(20); avg.Add(30);
            Assert.AreEqual(20.0, avg.Average, 1e-9);
            avg.Add(40);
            Assert.AreEqual(30.0, avg.Average, 1e-9);
            Assert.AreEqual(3, avg.Count);
        }

        [Test]
        public void RollingAverage_ClearResets()
        {
            var avg = new RollingAverage(3);
            avg.Add(5);
            avg.Clear();
            Assert.AreEqual(0, avg.Count);
            Assert.AreEqual(0.0, avg.Average);
        }

        [Test]
        public void RateMeter_SteadyTwentyFourFps()
        {
            var m = new RateMeter(8);
            double t = 100.0;
            for (int i = 0; i < 20; i++) { m.Tick(t); t += 1.0 / 24.0; }
            Assert.AreEqual(24.0, m.Rate(t), 0.01);
        }

        [Test]
        public void RateMeter_NeedsTwoTicks()
        {
            var m = new RateMeter(8);
            Assert.AreEqual(0.0, m.Rate(1.0));
            m.Tick(1.0);
            Assert.AreEqual(0.0, m.Rate(1.0));
        }

        [Test]
        public void RateMeter_GoesToZeroWhenStale()
        {
            var m = new RateMeter(8);
            m.Tick(1.0); m.Tick(1.1); m.Tick(1.2);
            Assert.Greater(m.Rate(1.3), 0.0);
            Assert.AreEqual(0.0, m.Rate(1.2 + RateMeter.StaleSeconds + 0.1));
        }

        [TestCase(640, 480, 640, 640, 480)]
        [TestCase(1920, 1080, 640, 640, 360)]
        [TestCase(1280, 720, 640, 640, 360)]
        [TestCase(1001, 751, 2000, 1000, 750)]
        public void HandImageSize_Choose(int w, int h, int max, int expectW, int expectH)
        {
            HandImageSize.Choose(w, h, max, out int ow, out int oh);
            Assert.AreEqual(expectW, ow);
            Assert.AreEqual(expectH, oh);
        }

        [Test]
        public void HandImageSize_InvalidInputGivesZero()
        {
            HandImageSize.Choose(0, 480, 640, out int w, out int h);
            Assert.AreEqual(0, w);
            Assert.AreEqual(0, h);
        }

        [Test]
        public void HandImageSize_NeverBelowTwo()
        {
            HandImageSize.Choose(4000, 1, 640, out int w, out int h);
            Assert.AreEqual(640, w);
            Assert.AreEqual(2, h);
        }

        [Test]
        public void HandsStatus_FormatsOnAndOff()
        {
            Assert.AreEqual("Hands 1, 24 fps, 18 ms", HandsStatus.Format(true, 1, 23.6, 17.5, ""));
            Assert.AreEqual("Hands off: model missing", HandsStatus.Format(false, 0, 0, 0, "model missing"));
            Assert.AreEqual("Hands off: unknown", HandsStatus.Format(false, 0, 0, 0, ""));
        }

        [Test]
        public void HandPose_ParseSide()
        {
            Assert.AreEqual(HandSide.Left, HandPose.ParseSide("Left"));
            Assert.AreEqual(HandSide.Right, HandPose.ParseSide("Right"));
            Assert.AreEqual(HandSide.Unknown, HandPose.ParseSide(null));
        }
    }
}
