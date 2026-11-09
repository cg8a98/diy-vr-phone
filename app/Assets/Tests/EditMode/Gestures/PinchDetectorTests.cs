using NUnit.Framework;
using UnityEngine;
using DiyVr.Gestures;

namespace DiyVr.Tests
{
    public class PinchDetectorTests
    {
        const float Scale = 0.1f;
        PinchDetector _d;
        int _started, _ended;

        [SetUp]
        public void SetUp()
        {
            _d = new PinchDetector();
            _started = 0;
            _ended = 0;
            _d.PinchStarted += () => _started++;
            _d.PinchEnded += () => _ended++;
        }

        // Feeds a frame where distance/scale == ratio.
        void Feed(float ratio, float time, float scale = Scale)
        {
            _d.Update(Vector3.zero, new Vector3(ratio * scale, 0f, 0f), scale, time);
        }

        void StartPinch(float t = 0f)
        {
            Feed(0.1f, t);
            Feed(0.1f, t + 0.06f);
            Assert.IsTrue(_d.IsPinching);
        }

        [Test]
        public void StartsBelowStartRatioAfterDebounce()
        {
            Feed(0.1f, 0f);
            Assert.IsFalse(_d.IsPinching);
            Feed(0.1f, 0.06f);
            Assert.IsTrue(_d.IsPinching);
            Assert.AreEqual(1, _started);
        }

        [Test]
        public void DoesNotStartAboveStartRatio()
        {
            for (int i = 0; i < 20; i++) Feed(0.3f, i * 0.02f);
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(0, _started);
        }

        [Test]
        public void EndsAboveEndRatioAfterDebounce()
        {
            StartPinch();
            Feed(0.6f, 1f);
            Assert.IsTrue(_d.IsPinching);
            Feed(0.6f, 1.06f);
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(1, _ended);
        }

        [Test]
        public void HysteresisKeepsStateBetweenThresholds()
        {
            // Not pinching, ratio between thresholds: stays off.
            for (int i = 0; i < 20; i++) Feed(0.33f, i * 0.02f);
            Assert.IsFalse(_d.IsPinching);

            StartPinch(1f);
            // Pinching, ratio between thresholds: stays on.
            for (int i = 0; i < 20; i++) Feed(0.33f, 2f + i * 0.02f);
            Assert.IsTrue(_d.IsPinching);
            Assert.AreEqual(1, _started);
            Assert.AreEqual(0, _ended);
        }

        [Test]
        public void DebounceRejectsOneFrameSpike()
        {
            Feed(0.6f, 0f);
            Feed(0.1f, 0.02f); // spike
            Feed(0.6f, 0.04f);
            Feed(0.6f, 0.10f);
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(0, _started);

            StartPinch(1f);
            Feed(0.6f, 2f); // spike open
            Feed(0.1f, 2.02f);
            Feed(0.1f, 2.10f);
            Assert.IsTrue(_d.IsPinching);
            Assert.AreEqual(0, _ended);
        }

        [Test]
        public void NoisyInputAroundStartThresholdDoesNotChatter()
        {
            // Alternates around StartRatio every frame at 60 Hz: never held 50 ms.
            for (int i = 0; i < 120; i++)
                Feed(i % 2 == 0 ? 0.22f : 0.28f, i / 60f);
            Assert.AreEqual(0, _started);
            Assert.AreEqual(0, _ended);
        }

        [Test]
        public void NoisyInputAroundEndThresholdDoesNotChatter()
        {
            StartPinch();
            for (int i = 0; i < 120; i++)
                Feed(i % 2 == 0 ? 0.37f : 0.43f, 1f + i / 60f);
            Assert.IsTrue(_d.IsPinching);
            Assert.AreEqual(1, _started);
            Assert.AreEqual(0, _ended);
        }

        [Test]
        public void StrengthMapsBetweenEndAndStartRatio()
        {
            Feed(0.5f, 0f);
            Assert.AreEqual(0f, _d.Strength, 1e-5f);
            Feed(0.40f, 0.1f);
            Assert.AreEqual(0f, _d.Strength, 1e-5f);
            Feed(0.325f, 0.2f);
            Assert.AreEqual(0.5f, _d.Strength, 1e-4f);
            Feed(0.25f, 0.3f);
            Assert.AreEqual(1f, _d.Strength, 1e-5f);
            Feed(0.0f, 0.4f);
            Assert.AreEqual(1f, _d.Strength, 1e-5f);
        }

        [Test]
        public void LostHandEndsPinchAfterTimeout()
        {
            StartPinch();
            float last = 0.06f;
            _d.UpdateLost(last + 0.1f);
            Assert.IsTrue(_d.IsPinching);
            _d.UpdateLost(last + 0.25f);
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(1, _ended);
            Assert.AreEqual(0f, _d.Strength, 1e-5f);
        }

        [Test]
        public void LostHandWithoutPinchDoesNothing()
        {
            _d.UpdateLost(5f);
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(0, _ended);
        }

        [Test]
        public void DataResumingBeforeTimeoutKeepsPinch()
        {
            StartPinch();
            _d.UpdateLost(0.16f);
            Feed(0.1f, 0.2f);
            _d.UpdateLost(0.3f);
            Assert.IsTrue(_d.IsPinching);
        }

        [Test]
        public void ResetEndsPinchAndClearsState()
        {
            StartPinch();
            _d.Reset();
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(1, _ended);
            Assert.AreEqual(0f, _d.Strength, 1e-5f);
            // Needs a fresh debounce after reset.
            Feed(0.1f, 10f);
            Assert.IsFalse(_d.IsPinching);
        }

        [Test]
        public void BadScaleIsIgnored()
        {
            foreach (var bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                _d.Update(Vector3.zero, Vector3.zero, bad, 0f);
                _d.Update(Vector3.zero, Vector3.zero, bad, 1f);
            }
            Assert.IsFalse(_d.IsPinching);
            Assert.AreEqual(0, _started);
        }

        [Test]
        public void BadPositionsAndTimeAreIgnored()
        {
            var nan = new Vector3(float.NaN, 0f, 0f);
            _d.Update(nan, Vector3.zero, Scale, 0f);
            _d.Update(Vector3.zero, nan, Scale, 1f);
            _d.Update(Vector3.zero, Vector3.zero, Scale, float.NaN);
            Assert.IsFalse(_d.IsPinching);
            Assert.IsTrue(float.IsNaN(_d.LastRatio));
        }

        [Test]
        public void BadInputDoesNotBreakActivePinch()
        {
            StartPinch();
            _d.Update(Vector3.zero, Vector3.one, 0f, 1f);
            _d.Update(Vector3.zero, Vector3.one, float.NaN, 1.1f);
            Assert.IsTrue(_d.IsPinching);
            Assert.AreEqual(0, _ended);
        }

        [Test]
        public void ScaleInvariant()
        {
            foreach (var scale in new[] { 0.05f, 0.1f, 8f, 100f })
            {
                var d = new PinchDetector();
                d.Update(Vector3.zero, new Vector3(0.1f * scale, 0f, 0f), scale, 0f);
                d.Update(Vector3.zero, new Vector3(0.1f * scale, 0f, 0f), scale, 0.06f);
                Assert.IsTrue(d.IsPinching, $"scale {scale}");
                d.Update(Vector3.zero, new Vector3(0.6f * scale, 0f, 0f), scale, 1f);
                d.Update(Vector3.zero, new Vector3(0.6f * scale, 0f, 0f), scale, 1.06f);
                Assert.IsFalse(d.IsPinching, $"scale {scale}");
            }
        }

        [Test]
        public void WorksInAnyDirection()
        {
            _d.Update(new Vector3(1f, 2f, 3f), new Vector3(1f, 2f + 0.01f, 3f), Scale, 0f);
            _d.Update(new Vector3(1f, 2f, 3f), new Vector3(1f, 2f, 3f + 0.01f), Scale, 0.06f);
            Assert.IsTrue(_d.IsPinching);
        }
    }
}
