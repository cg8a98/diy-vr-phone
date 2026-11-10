using DiyVr.Experiments;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class FiveButtonTrialTests
    {
        [Test]
        public void Hit_WrongAndMiss_AreCountedSeparately()
        {
            var t = new FiveButtonTrial(5, 1);
            int other = (t.Target + 1) % 5;
            Assert.AreEqual(TrialOutcome.Wrong, t.RecordPinch(other));
            Assert.AreEqual(TrialOutcome.Miss, t.RecordPinch(-1));
            Assert.AreEqual(TrialOutcome.Hit, t.RecordPinch(t.Target));
            Assert.AreEqual(1, t.Hits);
            Assert.AreEqual(1, t.Wrong);
            Assert.AreEqual(1, t.Misses);
            Assert.AreEqual(3, t.Pinches);
        }

        [Test]
        public void Target_NeverRepeats_AndStaysAfterWrongOrMiss()
        {
            var t = new FiveButtonTrial(5, 42);
            for (int i = 0; i < 500; i++)
            {
                int before = t.Target;
                t.RecordPinch(-1);
                Assert.AreEqual(before, t.Target);
                t.RecordPinch(t.Target);
                Assert.AreNotEqual(before, t.Target);
                Assert.That(t.Target, Is.InRange(0, 4));
            }
        }

        [Test]
        public void SameSeed_GivesSameTargets()
        {
            var a = new FiveButtonTrial(5, 7);
            var b = new FiveButtonTrial(5, 7);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(a.Target, b.Target);
                a.RecordPinch(a.Target);
                b.RecordPinch(b.Target);
            }
        }

        [Test]
        public void AllFiveButtons_AreEventuallyTargeted()
        {
            var t = new FiveButtonTrial(5, 3);
            var seen = new bool[5];
            for (int i = 0; i < 200; i++) { seen[t.Target] = true; t.RecordPinch(t.Target); }
            CollectionAssert.AreEqual(new[] { true, true, true, true, true }, seen);
        }

        [Test]
        public void Passed_NeedsNineOfLastTen()
        {
            var t = new FiveButtonTrial(5, 1);
            for (int i = 0; i < 9; i++) t.RecordPinch(t.Target);
            Assert.IsFalse(t.Passed, "only 9 pinches so far, window not full");
            t.RecordPinch(-1); // 9 hits + 1 miss in 10
            Assert.IsTrue(t.Passed);
            Assert.AreEqual(9, t.WindowHits);
        }

        [Test]
        public void Passed_FailsWithEightOfTen_AndWindowSlides()
        {
            var t = new FiveButtonTrial(5, 1);
            t.RecordPinch(-1);
            t.RecordPinch(-1);
            for (int i = 0; i < 8; i++) t.RecordPinch(t.Target);
            Assert.IsFalse(t.Passed);
            Assert.AreEqual(8, t.WindowHits);
            t.RecordPinch(t.Target); // oldest miss drops out: 9 hits + 1 miss
            Assert.IsTrue(t.Passed);
            t.RecordPinch(t.Target); // both misses gone: 10/10
            Assert.AreEqual(10, t.WindowHits);
            Assert.AreEqual(10, t.WindowCount);
            Assert.AreEqual(10, t.Hits);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var t = new FiveButtonTrial(5, 1);
            for (int i = 0; i < 12; i++) t.RecordPinch(t.Target);
            t.RecordPinch(-1);
            t.Reset();
            Assert.AreEqual(0, t.Pinches);
            Assert.AreEqual(0, t.WindowCount);
            Assert.IsFalse(t.Passed);
            Assert.That(t.Target, Is.InRange(0, 4));
        }

        [Test]
        public void OutOfRangeGaze_IsAMiss()
        {
            var t = new FiveButtonTrial(5, 1);
            Assert.AreEqual(TrialOutcome.Miss, t.RecordPinch(5));
        }

        [Test]
        public void Summary_ShowsCountsAndWindow()
        {
            var t = new FiveButtonTrial(5, 1);
            t.RecordPinch(t.Target);
            t.RecordPinch(-1);
            t.RecordPinch((t.Target + 1) % 5);
            Assert.AreEqual("Hits 1 / Wrong 1 / Miss 1 - last 10: 1/3", t.Summary());
        }

        [Test]
        public void TooFewButtons_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new FiveButtonTrial(1, 0));
        }
    }

    public class FiveButtonLayoutTests
    {
        [Test]
        public void Yaws_AreMinus30To30In15Steps()
        {
            Assert.AreEqual(-30f, FiveButtonLayout.YawDegrees(0), 1e-4f);
            Assert.AreEqual(0f, FiveButtonLayout.YawDegrees(2), 1e-4f);
            Assert.AreEqual(30f, FiveButtonLayout.YawDegrees(4), 1e-4f);
        }

        [Test]
        public void Buttons_AreOnArc_AtHeadHeight_MiddleStraightAhead()
        {
            Vector3 head = new Vector3(1f, 1.6f, 2f);
            Vector3 fwd = new Vector3(0f, -0.4f, 1f); // pitched down: must be ignored
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = FiveButtonLayout.ButtonPosition(head, fwd, i);
                Assert.AreEqual(1.6f, p.y, 1e-4f);
                Assert.AreEqual(1.5f, Vector3.Distance(head, p), 1e-4f);
            }
            Vector3 mid = FiveButtonLayout.ButtonPosition(head, fwd, 2);
            Assert.AreEqual(1f, mid.x, 1e-4f);
            Assert.AreEqual(3.5f, mid.z, 1e-4f);
        }

        [Test]
        public void RightButton_IsToTheRight()
        {
            Vector3 p = FiveButtonLayout.ButtonPosition(Vector3.zero, Vector3.forward, 4);
            Assert.Greater(p.x, 0f);
        }

        [Test]
        public void FlatForward_HandlesLookingStraightDown()
        {
            Assert.AreEqual(Vector3.forward, FiveButtonLayout.FlatForward(Vector3.down));
        }
    }

    public class PinchLandmarksTests
    {
        static Vector3[] Hand(Vector3 thumb, Vector3 index)
        {
            var lm = new Vector3[21];
            lm[0] = new Vector3(0.5f, 0.8f, 0f);
            lm[9] = new Vector3(0.5f, 0.6f, 0f);
            lm[4] = thumb;
            lm[8] = index;
            return lm;
        }

        [Test]
        public void ScaleIsWristToMiddleKnuckle()
        {
            Vector3 t, i; float s;
            Assert.IsTrue(PinchLandmarks.TryGet(Hand(Vector3.zero, Vector3.zero), 1f, out t, out i, out s));
            Assert.AreEqual(0.2f, s, 1e-5f);
        }

        [Test]
        public void AspectScalesX_AndZIsIgnored()
        {
            Vector3 t, i; float s;
            PinchLandmarks.TryGet(Hand(new Vector3(0.1f, 0.1f, 0.5f), new Vector3(0.2f, 0.1f, -0.5f)), 2f, out t, out i, out s);
            Assert.AreEqual(0.2f, (i - t).magnitude, 1e-5f);
        }

        [Test]
        public void BadInput_ReturnsFalse()
        {
            Vector3 t, i; float s;
            Assert.IsFalse(PinchLandmarks.TryGet(null, 1f, out t, out i, out s));
            Assert.IsFalse(PinchLandmarks.TryGet(new Vector3[5], 1f, out t, out i, out s));
            Assert.IsFalse(PinchLandmarks.TryGet(new Vector3[21], 1f, out t, out i, out s));
            Assert.IsFalse(PinchLandmarks.TryGet(Hand(Vector3.zero, Vector3.zero), 0f, out t, out i, out s));
        }
    }
}
