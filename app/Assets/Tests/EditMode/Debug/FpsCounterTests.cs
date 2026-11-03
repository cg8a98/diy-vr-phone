using DiyVr.Diagnostics;
using NUnit.Framework;

namespace DiyVr.Tests.Diagnostics
{
    public class FpsCounterTests
    {
        [Test]
        public void SteadyFrames_GiveExpectedFpsAndFrameTime()
        {
            var c = new FpsCounter(0.5f);
            bool updated = false;
            // 60 fps: 30 frames of 1/60 s fill 0.5 s.
            for (int i = 0; i < 30; i++)
                updated |= c.AddFrame(1f / 60f);
            Assert.IsTrue(updated);
            Assert.AreEqual(60f, c.Fps, 0.5f);
            Assert.AreEqual(16.67f, c.FrameMs, 0.1f);
        }

        [Test]
        public void BeforeFirstWindow_ValuesAreZero()
        {
            var c = new FpsCounter(0.5f);
            Assert.IsFalse(c.AddFrame(0.01f));
            Assert.AreEqual(0f, c.Fps);
            Assert.AreEqual(0f, c.FrameMs);
        }

        [Test]
        public void MixedFrameTimes_AverageOverWindow()
        {
            var c = new FpsCounter(0.5f);
            // 4 frames of 0.125 s = 0.5 s -> 8 fps, 125 ms.
            for (int i = 0; i < 4; i++)
                c.AddFrame(0.125f);
            Assert.AreEqual(8f, c.Fps, 0.001f);
            Assert.AreEqual(125f, c.FrameMs, 0.01f);
        }

        [Test]
        public void NewWindowReplacesOldValues()
        {
            var c = new FpsCounter(0.5f);
            for (int i = 0; i < 4; i++) c.AddFrame(0.125f);   // 8 fps
            for (int i = 0; i < 50; i++) c.AddFrame(0.01f);   // 100 fps
            Assert.AreEqual(100f, c.Fps, 0.5f);
        }

        [Test]
        public void InvalidDeltas_AreIgnored()
        {
            var c = new FpsCounter(0.5f);
            Assert.IsFalse(c.AddFrame(0f));
            Assert.IsFalse(c.AddFrame(-1f));
            Assert.IsFalse(c.AddFrame(float.NaN));
            Assert.IsFalse(c.AddFrame(float.PositiveInfinity));
            Assert.AreEqual(0f, c.Fps);
        }

        [Test]
        public void Reset_ClearsValues()
        {
            var c = new FpsCounter(0.5f);
            for (int i = 0; i < 4; i++) c.AddFrame(0.125f);
            c.Reset();
            Assert.AreEqual(0f, c.Fps);
            Assert.AreEqual(0f, c.FrameMs);
        }
    }
}
