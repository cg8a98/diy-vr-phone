using System.Collections.Generic;
using DiyVr.Hands;
using NUnit.Framework;

namespace DiyVr.Tests.Hands
{
    public class HandIdentityTests
    {
        private const float A = FakeHand.Aspect;
        private const float S = 0.1f;
        private const double Dt = 1.0 / 25.0;

        private static HandPose[] Seen(params HandPose[] h) { return h; }

        private static int[] Swap(int[] s) { return new[] { s[1], s[0] }; }

        [Test]
        public void NewHands_FollowTheLabel()
        {
            var id = new HandIdentity();
            CollectionAssert.AreEqual(new[] { 1, 0 }, id.Assign(
                Seen(FakeHand.At(0.9f, 0.6f, S, HandSide.Right), FakeHand.At(0.4f, 0.6f, S, HandSide.Left)), A, 0));

            Assert.AreEqual(1, new HandIdentity().Assign(Seen(FakeHand.At(0.9f, 0.6f, S, HandSide.Right)), A, 0)[0]);
            Assert.AreEqual(0, new HandIdentity().Assign(Seen(FakeHand.At(0.9f, 0.6f, S, HandSide.Left)), A, 0)[0]);
            Assert.AreEqual(0, new HandIdentity().Assign(Seen(FakeHand.At(0.9f, 0.6f, S, HandSide.Unknown)), A, 0)[0]);
        }

        [Test]
        public void NewHands_SameOrUnknownLabels_GetDifferentSlots()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 }, new HandIdentity().Assign(
                Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Right), FakeHand.At(0.9f, 0.6f, S, HandSide.Right)), A, 0));
            CollectionAssert.AreEqual(new[] { 0, 1 }, new HandIdentity().Assign(
                Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Unknown), FakeHand.At(0.9f, 0.6f, S, HandSide.Unknown)), A, 0));
            CollectionAssert.AreEqual(new[] { 1, 0 }, new HandIdentity().Assign(
                Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Unknown), FakeHand.At(0.9f, 0.6f, S, HandSide.Left)), A, 0));
        }

        [Test]
        public void LabelFlip_SamePositions_KeepsIdentity()
        {
            var id = new HandIdentity();
            id.Assign(Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Left), FakeHand.At(0.9f, 0.6f, S, HandSide.Right)), A, 0);

            // Both labels flipped.
            CollectionAssert.AreEqual(new[] { 0, 1 }, id.Assign(
                Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Right), FakeHand.At(0.9f, 0.6f, S, HandSide.Left)), A, Dt));
            // Both say "Left" (the case that put two skeletons on one hand).
            CollectionAssert.AreEqual(new[] { 0, 1 }, id.Assign(
                Seen(FakeHand.At(0.41f, 0.6f, S, HandSide.Left), FakeHand.At(0.9f, 0.61f, S, HandSide.Left)), A, 2 * Dt));
            // Labels gone.
            CollectionAssert.AreEqual(new[] { 0, 1 }, id.Assign(
                Seen(FakeHand.At(0.41f, 0.6f, S, HandSide.Unknown), FakeHand.At(0.9f, 0.61f, S, HandSide.Unknown)), A, 3 * Dt));
        }

        [Test]
        public void SingleHand_LabelFlip_KeepsItsSlot()
        {
            var id = new HandIdentity();
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Left)), A, 0)[0]);
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.51f, 0.6f, S, HandSide.Right)), A, Dt)[0]);
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.52f, 0.6f, S, HandSide.Left)), A, 2 * Dt)[0]);
        }

        [Test]
        public void ResultOrderSwap_KeepsIdentity()
        {
            var id = new HandIdentity();
            id.Assign(Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Left), FakeHand.At(0.9f, 0.6f, S, HandSide.Right)), A, 0);
            CollectionAssert.AreEqual(new[] { 1, 0 }, id.Assign(
                Seen(FakeHand.At(0.9f, 0.6f, S, HandSide.Left), FakeHand.At(0.4f, 0.6f, S, HandSide.Left)), A, Dt));
        }

        [Test]
        public void HandsCrossingSlowly_KeepIdentity()
        {
            var id = new HandIdentity();
            // The left hand starts on the left and moves right, the right hand the other way, passing just above it
            // (1.2 hand lengths apart at the closest point, so both are close enough to be confused).
            for (int i = 0; i <= 30; i++)
            {
                float t = i / 30f;
                // MediaPipe is free to call them whatever it likes after the crossing: flip the labels half way.
                bool flip = i > 15;
                HandPose left = FakeHand.At(0.3f + 0.6f * t, 0.5f, S, flip ? HandSide.Right : HandSide.Left);
                HandPose right = FakeHand.At(0.9f - 0.6f * t, 0.62f, S, flip ? HandSide.Left : HandSide.Right);
                // The order in the result alternates too.
                int[] slots = i % 2 == 0
                    ? id.Assign(Seen(left, right), A, i * Dt)
                    : Swap(id.Assign(Seen(right, left), A, i * Dt));
                CollectionAssert.AreEqual(new[] { 0, 1 }, slots, "step " + i);
            }
        }

        [Test]
        public void NewHandAppears_ExistingHandKeepsSlot_NewTakesTheFreeOne()
        {
            var id = new HandIdentity();
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Right)), A, 0)[0]);

            // A second hand shows up, also labelled Right and listed first.
            CollectionAssert.AreEqual(new[] { 0, 1 }, id.Assign(
                Seen(FakeHand.At(1.1f, 0.6f, S, HandSide.Right), FakeHand.At(0.5f, 0.6f, S, HandSide.Right)), A, Dt));
            // And both stay put afterwards.
            CollectionAssert.AreEqual(new[] { 1, 0 }, id.Assign(
                Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Left), FakeHand.At(1.1f, 0.6f, S, HandSide.Right)), A, 2 * Dt));
        }

        [Test]
        public void OneHandDropsOutForAFrame_BothKeepIdentity()
        {
            var id = new HandIdentity();
            id.Assign(Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Left), FakeHand.At(0.9f, 0.6f, S, HandSide.Right)), A, 0);
            // Only the right hand is detected, mislabelled.
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(0.9f, 0.6f, S, HandSide.Left)), A, Dt)[0]);
            // Both are back, both mislabelled.
            CollectionAssert.AreEqual(new[] { 0, 1 }, id.Assign(
                Seen(FakeHand.At(0.4f, 0.6f, S, HandSide.Right), FakeHand.At(0.9f, 0.6f, S, HandSide.Left)), A, 2 * Dt));
        }

        [Test]
        public void JumpBeyondMax_IsANewHand()
        {
            var id = new HandIdentity();
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.3f, 0.6f, S, HandSide.Left)), A, 0)[0]);
            // Inside the limit: the same hand even with the other label.
            float inside = 0.3f + HandIdentity.MaxJumpRatio * S * 0.9f;
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(inside, 0.6f, S, HandSide.Right)), A, Dt)[0]);
            // Far outside the limit: a different hand; it takes its label's slot.
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(inside + 0.6f, 0.6f, S, HandSide.Right)), A, 2 * Dt)[0]);
        }

        [Test]
        public void NewHandWithTakenLabel_UsesTheFreeSlot()
        {
            var id = new HandIdentity();
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.3f, 0.6f, S, HandSide.Left)), A, 0)[0]);
            // Another hand far away, also "Left", while the first is missing for one frame.
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(1.1f, 0.6f, S, HandSide.Left)), A, Dt)[0]);
            // The first one is back: it still owns slot 0.
            CollectionAssert.AreEqual(new[] { 0, 1 }, id.Assign(
                Seen(FakeHand.At(0.3f, 0.6f, S, HandSide.Right), FakeHand.At(1.1f, 0.6f, S, HandSide.Left)), A, 2 * Dt));
        }

        [Test]
        public void AfterMemoryRunsOut_TheLabelDecidesAgain()
        {
            var id = new HandIdentity();
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Left)), A, 0)[0]);
            double later = HandIdentity.MemorySeconds + 0.1;
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Right)), A, later)[0]);
        }

        [Test]
        public void Reset_ForgetsTheHands()
        {
            var id = new HandIdentity();
            Assert.AreEqual(0, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Left)), A, 0)[0]);
            id.Reset();
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Right)), A, Dt)[0]);
        }

        [Test]
        public void EmptyNullAndExtraHands()
        {
            var id = new HandIdentity();
            Assert.AreEqual(0, id.Assign(null, A, 0).Length);
            Assert.AreEqual(0, id.Assign(new HandPose[0], A, 0).Length);
            int[] slots = id.Assign(Seen(
                FakeHand.At(0.3f, 0.6f, S, HandSide.Left), FakeHand.At(0.7f, 0.6f, S, HandSide.Right),
                FakeHand.At(1.1f, 0.6f, S, HandSide.Right)), A, 0);
            CollectionAssert.AreEqual(new[] { 0, 1, -1 }, slots);
        }

        [Test]
        public void DedupeThenIdentity_OneRealHandDetectedTwice_UsesOneSlotOnly()
        {
            var id = new HandIdentity();
            Assert.AreEqual(1, id.Assign(Seen(FakeHand.At(0.5f, 0.6f, S, HandSide.Right, 0.9f)), A, 0)[0]);

            HandPose[] twice = Seen(
                FakeHand.At(0.5f, 0.6f, S, HandSide.Left, 0.5f), FakeHand.At(0.505f, 0.6f, S, HandSide.Right, 0.9f));
            IReadOnlyList<HandPose> hands = HandDedupe.Suppress(twice, A);
            Assert.AreEqual(1, hands.Count);
            CollectionAssert.AreEqual(new[] { 1 }, id.Assign(hands, A, Dt));
        }
    }
}
