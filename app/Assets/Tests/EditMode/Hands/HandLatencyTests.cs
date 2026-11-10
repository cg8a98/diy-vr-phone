using DiyVr.Hands;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests.Hands
{
    public class HandLatencyTests
    {
        [Test]
        public void Age_IsNowMinusStamp()
        {
            Assert.AreEqual(0.040, HandLatency.AgeSeconds(100.000, 100.040), 1e-9);
        }

        [Test]
        public void Age_UnknownWhenStampMissingOrOnAnotherClock()
        {
            Assert.AreEqual(-1.0, HandLatency.AgeSeconds(0.0, 100.0));
            Assert.AreEqual(-1.0, HandLatency.AgeSeconds(50.0, 100.0));   // 50 s old: wrong clock
            Assert.AreEqual(-1.0, HandLatency.AgeSeconds(200.0, 100.0));  // from the future
        }

        [Test]
        public void Age_TinyNegativeIsClampedToZero()
        {
            Assert.AreEqual(0.0, HandLatency.AgeSeconds(100.002, 100.0), 1e-9);
        }

        [Test]
        public void IsNewFrame_RejectsSameStamp_AcceptsNewer()
        {
            Assert.IsTrue(HandLatency.IsNewFrame(-1, 10.0));      // first frame
            Assert.IsFalse(HandLatency.IsNewFrame(10.0, 10.0));   // same camera image again
            Assert.IsFalse(HandLatency.IsNewFrame(10.0, 9.9));    // older
            Assert.IsTrue(HandLatency.IsNewFrame(10.0, 10.033));
        }

        [Test]
        public void IsNewFrame_FramesWithoutStampAlwaysCount()
        {
            Assert.IsTrue(HandLatency.IsNewFrame(10.0, 0.0));
        }

        [Test]
        public void Lead_IsHalfTheAge_CappedAndNeverNegative()
        {
            Assert.AreEqual(0f, HandLatency.LeadSeconds(-1.0));
            Assert.AreEqual(0f, HandLatency.LeadSeconds(0.0));
            Assert.AreEqual(0.03f, HandLatency.LeadSeconds(0.06), 1e-6f);
            Assert.AreEqual(HandLatency.MaxLeadSeconds, HandLatency.LeadSeconds(0.5), 1e-6f);
        }

        [Test]
        public void Lead_WithExtrapolator_StaysWithinOneIntervalAndOnTheVelocity()
        {
            var prev = Vector3.zero;
            var cur = new Vector3(0.1f, 0f, 0f);
            // 33 ms between camera frames, result is 80 ms old: lead is 40 ms, which Predict then caps at one interval (33 ms).
            Vector3 p = HandExtrapolator.Predict(prev, cur, 0.033f, HandLatency.LeadSeconds(0.08));
            Assert.AreEqual(0.2f, p.x, 1e-5f); // exactly one interval ahead, no more
            // Stationary hand: no movement, no overshoot.
            Vector3 still = HandExtrapolator.Predict(cur, cur, 0.033f, 0.05f);
            Assert.AreEqual(cur.x, still.x, 1e-6f);
        }

        [Test]
        public void FormatLag_ShowsBreakdown_AndPostIsTheRemainder()
        {
            Assert.AreEqual("Lag 85 ms = cam 40 + cv 4 + inf 30 + post 11", HandLatency.FormatLag(85, 40, 4, 30));
        }

        [Test]
        public void FormatLag_UnknownAgeShowsQuestionMark_PostNeverNegative()
        {
            Assert.AreEqual("Lag 30 ms = cam ? + cv 4 + inf 30 + post 0", HandLatency.FormatLag(30, -1, 4, 30));
        }

        [Test]
        public void StatusLine_AddsLagLineOnlyWhenMeasured()
        {
            HandsStatus.SetOff("x");
            Assert.AreEqual("Hands off: x", HandsStatus.Line());
            HandsStatus.Active = true;
            HandsStatus.HandCount = 1;
            HandsStatus.LandmarkFps = 24;
            HandsStatus.InferenceMs = 30;
            Assert.AreEqual("Hands 1, 24 fps, 30 ms", HandsStatus.Line());
            HandsStatus.LagTotalMs = 85;
            HandsStatus.LagAgeMs = 40;
            HandsStatus.LagConvertMs = 4;
            Assert.AreEqual("Hands 1, 24 fps, 30 ms\nLag 85 ms = cam 40 + cv 4 + inf 30 + post 11", HandsStatus.Line());
            HandsStatus.SetOff("done");
        }
    }
}
