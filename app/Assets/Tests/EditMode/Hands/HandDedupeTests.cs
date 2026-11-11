using System.Collections.Generic;
using DiyVr.Hands;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests.Hands
{
    /// <summary>Builds simple fake hands for the dedupe and identity tests.</summary>
    internal static class FakeHand
    {
        public const float Aspect = 4f / 3f;

        /// <summary>
        /// An open hand whose wrist is at (x, y) in image-height units (so normalised x = x / aspect) and whose
        /// wrist-to-middle-knuckle length is exactly <paramref name="scale"/>. Fingers point up the image.
        /// </summary>
        public static HandPose At(float x, float y, float scale, HandSide side, float score = 0.9f, float aspect = Aspect)
        {
            var pts = new Vector3[HandPose.LandmarkCount];
            pts[0] = new Vector3(x / aspect, y, 0f);
            float[] fingerX = { -0.5f, -0.25f, 0f, 0.25f, 0.5f };
            float[] jointY = { -1f, -1.4f, -1.7f, -1.9f };
            for (int f = 0; f < 5; f++)
                for (int j = 0; j < 4; j++)
                    pts[1 + f * 4 + j] = new Vector3((x + fingerX[f] * scale) / aspect, y + jointY[j] * scale, 0f);
            return new HandPose(pts, side, score);
        }
    }

    public class HandDedupeTests
    {
        private const float A = FakeHand.Aspect;
        private const float S = 0.1f;

        [Test]
        public void HandScale_IsWristToMiddleKnuckle_WithAspect()
        {
            Assert.AreEqual(S, HandDedupe.HandScale(FakeHand.At(0.5f, 0.6f, S, HandSide.Left).Landmarks, A), 1e-5f);
        }

        [Test]
        public void HandScale_BadInputGivesMinimum()
        {
            Assert.AreEqual(HandDedupe.MinHandScale, HandDedupe.HandScale(null, A));
            Assert.AreEqual(HandDedupe.MinHandScale, HandDedupe.HandScale(new Vector3[HandPose.LandmarkCount], A));
        }

        [Test]
        public void MeanDistance_OfShiftedHandIsTheShift()
        {
            HandPose a = FakeHand.At(0.5f, 0.6f, S, HandSide.Left);
            HandPose b = FakeHand.At(0.5f + 0.03f, 0.6f - 0.04f, S, HandSide.Left);
            Assert.AreEqual(0.05f, HandDedupe.MeanDistance(a.Landmarks, b.Landmarks, A), 1e-5f);
        }

        [Test]
        public void TwoNearIdenticalHands_BecomeOne_HigherScoreStays()
        {
            HandPose low = FakeHand.At(0.5f, 0.6f, S, HandSide.Left, 0.6f);
            HandPose high = FakeHand.At(0.5f + 0.1f * S, 0.6f, S, HandSide.Right, 0.8f);

            IReadOnlyList<HandPose> r = HandDedupe.Suppress(new[] { low, high }, A);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(high, r[0]);

            r = HandDedupe.Suppress(new[] { high, low }, A);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(high, r[0]);
        }

        [Test]
        public void ScoreTie_KeepsTheFirst()
        {
            HandPose first = FakeHand.At(0.5f, 0.6f, S, HandSide.Left, 0.7f);
            HandPose second = FakeHand.At(0.5f, 0.6f + 0.05f * S, S, HandSide.Right, 0.7f);
            IReadOnlyList<HandPose> r = HandDedupe.Suppress(new[] { first, second }, A);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(first, r[0]);
        }

        [Test]
        public void TwoSeparateHands_StayTwo_SameList()
        {
            var hands = new[] { FakeHand.At(0.4f, 0.6f, S, HandSide.Left), FakeHand.At(0.9f, 0.6f, S, HandSide.Right) };
            IReadOnlyList<HandPose> r = HandDedupe.Suppress(hands, A);
            Assert.AreEqual(2, r.Count);
            Assert.AreSame(hands, r);
        }

        [Test]
        public void TwoHandsSideBySide_Touching_StayTwo()
        {
            // One palm width apart: the bounding boxes touch, but it is clearly two hands.
            var hands = new[] { FakeHand.At(0.5f, 0.6f, S, HandSide.Left), FakeHand.At(0.5f + 0.9f * S, 0.6f, S, HandSide.Right) };
            Assert.AreEqual(2, HandDedupe.Suppress(hands, A).Count);
        }

        [Test]
        public void Threshold_IsInHandLengths()
        {
            HandPose a = FakeHand.At(0.5f, 0.6f, S, HandSide.Left);
            float t = HandDedupe.DuplicateDistanceRatio * S;
            Assert.IsTrue(HandDedupe.IsDuplicate(a, FakeHand.At(0.5f + t * 0.9f, 0.6f, S, HandSide.Left), A));
            Assert.IsFalse(HandDedupe.IsDuplicate(a, FakeHand.At(0.5f + t * 1.1f, 0.6f, S, HandSide.Left), A));
        }

        [Test]
        public void SameOffset_NearHandIsDuplicate_FarHandIsNot()
        {
            const float offset = 0.03f;
            Assert.IsTrue(HandDedupe.IsDuplicate(
                FakeHand.At(0.5f, 0.6f, 0.2f, HandSide.Left), FakeHand.At(0.5f + offset, 0.6f, 0.2f, HandSide.Left), A));
            Assert.IsFalse(HandDedupe.IsDuplicate(
                FakeHand.At(0.5f, 0.6f, 0.05f, HandSide.Left), FakeHand.At(0.5f + offset, 0.6f, 0.05f, HandSide.Left), A));
        }

        [Test]
        public void HorizontalDistance_IsScaledByAspect()
        {
            // The same normalised landmarks: on a square image the shift is 0.25 hand lengths, on a 2:1 image twice that.
            HandPose a = FakeHand.At(0.5f, 0.6f, S, HandSide.Left, 0.9f, 1f);
            var shifted = new Vector3[HandPose.LandmarkCount];
            for (int i = 0; i < shifted.Length; i++) shifted[i] = a.Landmarks[i] + new Vector3(0.25f * S, 0f, 0f);
            HandPose b = new HandPose(shifted, HandSide.Left, 0.9f);
            Assert.IsTrue(HandDedupe.IsDuplicate(a, b, 1f));
            Assert.AreEqual(0.25f * S, HandDedupe.MeanDistance(a.Landmarks, b.Landmarks, 1f), 1e-5f);
            Assert.AreEqual(0.5f * S, HandDedupe.MeanDistance(a.Landmarks, b.Landmarks, 2f), 1e-5f);
        }

        [Test]
        public void ThreeDetections_OneDuplicate_KeepsTwoInOrder()
        {
            HandPose a = FakeHand.At(0.4f, 0.6f, S, HandSide.Left, 0.5f);
            HandPose other = FakeHand.At(0.9f, 0.6f, S, HandSide.Right, 0.9f);
            HandPose aAgain = FakeHand.At(0.4f + 0.05f * S, 0.6f, S, HandSide.Right, 0.8f);
            IReadOnlyList<HandPose> r = HandDedupe.Suppress(new[] { a, other, aAgain }, A);
            Assert.AreEqual(2, r.Count);
            Assert.AreSame(other, r[0]);
            Assert.AreSame(aAgain, r[1]);
        }

        [Test]
        public void EmptyNullAndSingle_AreReturnedAsIs()
        {
            Assert.IsNull(HandDedupe.Suppress(null, A));
            var none = new HandPose[0];
            Assert.AreSame(none, HandDedupe.Suppress(none, A));
            var one = new[] { FakeHand.At(0.5f, 0.6f, S, HandSide.Left) };
            Assert.AreSame(one, HandDedupe.Suppress(one, A));
        }

        [Test]
        public void BadAspect_FallsBackToSquare()
        {
            var hands = new[]
            {
                FakeHand.At(0.5f, 0.6f, S, HandSide.Left, 0.9f, 1f), FakeHand.At(0.5f, 0.6f, S, HandSide.Right, 0.8f, 1f),
            };
            Assert.AreEqual(1, HandDedupe.Suppress(hands, 0f).Count);
            Assert.AreEqual(1, HandDedupe.Suppress(hands, float.NaN).Count);
        }
    }
}
