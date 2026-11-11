using NUnit.Framework;
using UnityEngine;
using DiyVr.Gestures;
using DiyVr.Hands;

namespace DiyVr.Tests
{
    public class PinchSlotsTests
    {
        const float Scale = 0.1f;

        static HandPose Hand(HandSide side)
        {
            return new HandPose(new Vector3[HandPose.LandmarkCount], side, 1f);
        }

        static HandPose[] Hands(params HandSide[] sides)
        {
            var hands = new HandPose[sides.Length];
            for (int i = 0; i < sides.Length; i++) hands[i] = Hand(sides[i]);
            return hands;
        }

        static void AssertNeverShared(int[] d)
        {
            foreach (int x in d) Assert.IsTrue(x == 0 || x == 1, "detector index must be 0 or 1, was " + x);
            if (d.Length == 2) Assert.AreNotEqual(d[0], d[1], "two hands share a detector");
        }

        // ---- slots used when valid ----

        [Test]
        public void ValidSlots_AreUsed()
        {
            CollectionAssert.AreEqual(new[] { 0, 1 },
                PinchSlots.Assign(Hands(HandSide.Left, HandSide.Right), new[] { 0, 1 }));
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new[] { 1, 0 }));
        }

        [Test]
        public void ValidSlots_WinOverTheLabel()
        {
            // Labels say Left/Right but the slots say the opposite: the slots decide.
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Left, HandSide.Right), new[] { 1, 0 }));
            // One hand labelled Left sitting in slot 1.
            CollectionAssert.AreEqual(new[] { 1 }, PinchSlots.Assign(Hands(HandSide.Left), new[] { 1 }));
        }

        [Test]
        public void ThreeHands_OnlyFirstTwoGetADetector_AndSlotsStillUsed()
        {
            // HandIdentity gives a third hand -1; that must not throw the first two back onto the label.
            int[] d = PinchSlots.Assign(Hands(HandSide.Left, HandSide.Right, HandSide.Right), new[] { 1, 0, -1 });
            CollectionAssert.AreEqual(new[] { 1, 0 }, d);
        }

        // ---- label flip with stable slots keeps the same detector ----

        [Test]
        public void LabelFlip_WithStableSlots_KeepsTheSameDetector()
        {
            int[] slots = { 0, 1 };
            int[] before = PinchSlots.Assign(Hands(HandSide.Left, HandSide.Right), slots);
            int[] bothRight = PinchSlots.Assign(Hands(HandSide.Right, HandSide.Right), slots);
            int[] swapped = PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), slots);
            int[] unknown = PinchSlots.Assign(Hands(HandSide.Unknown, HandSide.Unknown), slots);
            CollectionAssert.AreEqual(before, bothRight);
            CollectionAssert.AreEqual(before, swapped);
            CollectionAssert.AreEqual(before, unknown);
        }

        [Test]
        public void SingleHand_LabelFlip_KeepsTheSameDetector()
        {
            int[] slots = { 0 };
            Assert.AreEqual(0, PinchSlots.Assign(Hands(HandSide.Left), slots)[0]);
            Assert.AreEqual(0, PinchSlots.Assign(Hands(HandSide.Right), slots)[0]);
        }

        [Test]
        public void HeldPinch_SurvivesALabelFlip()
        {
            // End to end with real detectors: hand A (slot 0) holds a pinch, hand B (slot 1) is open. For a few
            // results the labels swap. Choosing by label would feed A's detector B's open hand and B's detector a pinch.
            var detectors = new[] { new PinchDetector(), new PinchDetector() };
            int started = 0, ended = 0;
            foreach (var det in detectors) { det.PinchStarted += () => started++; det.PinchEnded += () => ended++; }
            float[] ratios = { 0.1f, 0.8f }; // hand A pinched, hand B open
            int[] slots = { 0, 1 };

            for (int frame = 0; frame < 12; frame++)
            {
                float t = frame * 0.04f;
                bool flipped = frame >= 5 && frame < 9;
                HandPose[] hands = flipped ? Hands(HandSide.Right, HandSide.Left) : Hands(HandSide.Left, HandSide.Right);
                int[] d = PinchSlots.Assign(hands, slots);
                for (int h = 0; h < d.Length; h++)
                    detectors[d[h]].Update(Vector3.zero, new Vector3(ratios[h] * Scale, 0f, 0f), Scale, t);
            }

            Assert.IsTrue(detectors[0].IsPinching, "the held pinch was lost");
            Assert.IsFalse(detectors[1].IsPinching, "the open hand fired a false pinch");
            Assert.AreEqual(1, started);
            Assert.AreEqual(0, ended);
        }

        // ---- fallback on bad slot data ----

        [Test]
        public void NullSlots_FallBackToTheLabel()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), null));
        }

        [Test]
        public void SlotCountMismatch_FallsBackToTheLabel()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new int[0]));
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new[] { 0 }));
            CollectionAssert.AreEqual(new[] { 1 },
                PinchSlots.Assign(Hands(HandSide.Right), new[] { 0, 1 }));
        }

        [Test]
        public void NoSlotOrOutOfRange_FallsBackToTheLabel()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new[] { 0, -1 }));
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new[] { 0, 2 }));
            CollectionAssert.AreEqual(new[] { 1 }, PinchSlots.Assign(Hands(HandSide.Right), new[] { -1 }));
        }

        [Test]
        public void DuplicateSlots_FallBackToTheLabel()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new[] { 0, 0 }));
            CollectionAssert.AreEqual(new[] { 1, 0 },
                PinchSlots.Assign(Hands(HandSide.Right, HandSide.Left), new[] { 1, 1 }));
        }

        [Test]
        public void Fallback_MatchesTheOldLabelLogic()
        {
            // Left 0, Right 1, unknown = position in the list, taken -> the other one.
            CollectionAssert.AreEqual(new[] { 0 }, PinchSlots.Assign(Hands(HandSide.Left), null));
            CollectionAssert.AreEqual(new[] { 1 }, PinchSlots.Assign(Hands(HandSide.Right), null));
            CollectionAssert.AreEqual(new[] { 0 }, PinchSlots.Assign(Hands(HandSide.Unknown), null));
            CollectionAssert.AreEqual(new[] { 0, 1 }, PinchSlots.Assign(Hands(HandSide.Unknown, HandSide.Unknown), null));
            CollectionAssert.AreEqual(new[] { 1, 0 }, PinchSlots.Assign(Hands(HandSide.Right, HandSide.Right), null));
            CollectionAssert.AreEqual(new[] { 0, 1 }, PinchSlots.Assign(Hands(HandSide.Left, HandSide.Left), null));
            CollectionAssert.AreEqual(new[] { 1, 0 }, PinchSlots.Assign(Hands(HandSide.Right, HandSide.Unknown), null));
        }

        // ---- two hands never share a detector ----

        [Test]
        public void TwoHands_NeverShareADetector()
        {
            HandSide[] sides = { HandSide.Unknown, HandSide.Left, HandSide.Right };
            int[][] slotCases =
            {
                null, new int[0], new[] { 0 }, new[] { 0, 1 }, new[] { 1, 0 }, new[] { 0, 0 }, new[] { 1, 1 },
                new[] { -1, -1 }, new[] { 0, -1 }, new[] { -1, 1 }, new[] { 2, 0 }, new[] { 0, 1, -1 }, new[] { 5, 5 },
            };
            foreach (HandSide a in sides)
                foreach (HandSide b in sides)
                    foreach (int[] slots in slotCases)
                    {
                        int[] d = PinchSlots.Assign(Hands(a, b), slots);
                        Assert.AreEqual(2, d.Length);
                        AssertNeverShared(d);
                    }
        }

        [Test]
        public void NoHands_OrNull_GiveAnEmptyResult()
        {
            Assert.AreEqual(0, PinchSlots.Assign(null, null).Length);
            Assert.AreEqual(0, PinchSlots.Assign(new HandPose[0], new int[0]).Length);
            Assert.AreEqual(0, PinchSlots.Assign(new HandPose[0], new[] { 0, 1 }).Length);
        }

        [Test]
        public void NullHandEntry_IsTreatedAsUnknown()
        {
            int[] d = PinchSlots.Assign(new HandPose[] { null, Hand(HandSide.Left) }, null);
            CollectionAssert.AreEqual(new[] { 0, 1 }, d);
        }
    }
}
