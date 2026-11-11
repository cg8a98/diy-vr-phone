using System.Collections.Generic;
using DiyVr.Hands;

namespace DiyVr.Gestures
{
    /// <summary>
    /// Chooses which of the two PinchDetectors each detected hand feeds. Pure maths: no Unity objects.
    ///
    /// Normally the detector is the hand's stable slot (HandTrackingService.LatestSlots, made by HandIdentity), which
    /// follows the hand by where it is. MediaPipe's Left/Right label flips now and then for a frame; choosing by the
    /// label would hand one hand's pinch state to the other detector, which loses a pinch or fires a false one.
    ///
    /// The label is only used when the slots cannot be trusted for this result: none given, a different count than the
    /// hands, a slot that is not 0 or 1, or two hands in the same slot.
    /// </summary>
    public static class PinchSlots
    {
        /// <summary>Number of pinch detectors (one per hand).</summary>
        public const int DetectorCount = 2;

        /// <summary>
        /// Returns the detector index (0 or 1) for each of the first <see cref="DetectorCount"/> hands, in the same
        /// order as <paramref name="hands"/>. No two hands ever get the same detector. Hands beyond the first two are
        /// left out (the result is never longer than <see cref="DetectorCount"/>).
        /// </summary>
        public static int[] Assign(IReadOnlyList<HandPose> hands, IReadOnlyList<int> slots)
        {
            int count = hands == null ? 0 : hands.Count;
            int n = count < DetectorCount ? count : DetectorCount;
            int[] result = new int[n];
            if (n == 0) return result;

            if (SlotsUsable(slots, count, n))
            {
                for (int h = 0; h < n; h++) result[h] = slots[h];
                return result;
            }

            // Fallback, the logic used before the stable slots existed: Left 0, Right 1, unknown = position in the
            // list; a hand whose detector is already taken gets the other one.
            bool[] taken = new bool[DetectorCount];
            for (int h = 0; h < n; h++)
            {
                HandSide side = hands[h] != null ? hands[h].Side : HandSide.Unknown;
                int d = side == HandSide.Right ? 1 : side == HandSide.Left ? 0 : h;
                if (taken[d]) d = 1 - d;
                taken[d] = true;
                result[h] = d;
            }
            return result;
        }

        /// <summary>
        /// True when <paramref name="slots"/> can pick the detectors: one entry per hand, and the first
        /// <paramref name="used"/> entries are each 0 or 1 and all different.
        /// </summary>
        private static bool SlotsUsable(IReadOnlyList<int> slots, int handCount, int used)
        {
            if (slots == null || slots.Count != handCount) return false;
            bool[] taken = new bool[DetectorCount];
            for (int h = 0; h < used; h++)
            {
                int s = slots[h];
                if (s < 0 || s >= DetectorCount || taken[s]) return false;
                taken[s] = true;
            }
            return true;
        }
    }
}
