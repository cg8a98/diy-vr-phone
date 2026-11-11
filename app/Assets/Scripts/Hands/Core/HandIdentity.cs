using System.Collections.Generic;
using UnityEngine;

namespace DiyVr.Hands
{
    /// <summary>
    /// Gives each detected hand one of two fixed slots (0 = the "left" skeleton, 1 = the "right" skeleton) and keeps a
    /// hand in its slot from frame to frame by where it is, not by MediaPipe's Left/Right label. The label flips now and
    /// then for a frame, which used to swap the two skeleton colours or put both on one hand.
    ///
    /// Each new hand is matched to the slot whose last palm centre is nearest, as long as it moved less than
    /// <see cref="MaxJumpRatio"/> hand lengths and the slot was seen within <see cref="MemorySeconds"/>. A hand that
    /// matches nothing is new and takes the slot of its label (Left 0, Right 1) when that slot is free.
    /// Pure maths: no Unity objects.
    /// </summary>
    public sealed class HandIdentity
    {
        public const int SlotCount = 2;

        /// <summary>Furthest a palm may move between two results and still be the same hand, in hand lengths.</summary>
        public const float MaxJumpRatio = 2.0f;

        /// <summary>How long a slot remembers a hand that is no longer detected.</summary>
        public const double MemorySeconds = 0.5;

        private readonly Vector2[] _centre = new Vector2[SlotCount];
        private readonly double[] _seen = { double.NegativeInfinity, double.NegativeInfinity };

        /// <summary>Forgets both hands (tracking restarted).</summary>
        public void Reset()
        {
            for (int s = 0; s < SlotCount; s++) _seen[s] = double.NegativeInfinity;
        }

        /// <summary>
        /// Returns the slot for each hand, in the same order as <paramref name="hands"/>; -1 for a hand that got no slot
        /// (only possible with more than two hands). aspect = image width / height; now in seconds.
        /// </summary>
        public int[] Assign(IReadOnlyList<HandPose> hands, float aspect, double now)
        {
            int count = hands == null ? 0 : hands.Count;
            int[] slots = new int[count];
            for (int i = 0; i < count; i++) slots[i] = -1;
            if (count == 0) return slots;
            if (!(aspect > 0f)) aspect = 1f;

            int n = Mathf.Min(count, SlotCount);
            var centres = new Vector2[n];
            // cost[h, s]: distance from hand h to slot s, or infinity when that cannot be the same hand.
            var cost = new float[n, SlotCount];
            bool[] fresh = new bool[SlotCount];
            for (int s = 0; s < SlotCount; s++) fresh[s] = now - _seen[s] <= MemorySeconds;
            for (int h = 0; h < n; h++)
            {
                centres[h] = HandDedupe.PalmCentre(hands[h].Landmarks, aspect);
                float maxJump = MaxJumpRatio * HandDedupe.HandScale(hands[h].Landmarks, aspect);
                for (int s = 0; s < SlotCount; s++)
                {
                    float d = Vector2.Distance(centres[h], _centre[s]);
                    cost[h, s] = fresh[s] && d <= maxJump ? d : float.PositiveInfinity;
                }
            }

            if (n == 1)
            {
                int best = cost[0, 1] < cost[0, 0] ? 1 : 0;
                if (float.IsInfinity(cost[0, best]))
                {
                    // New hand: its label's slot, unless another hand was just there and the other slot is free.
                    best = PreferredSlot(hands[0].Side, 0);
                    if (fresh[best] && !fresh[1 - best]) best = 1 - best;
                }
                slots[0] = best;
            }
            else
            {
                // Two hands, two slots: straight (hand 0 -> slot 0) or crossed. More matches wins, then less movement.
                int straightMatches = Matches(cost[0, 0]) + Matches(cost[1, 1]);
                int crossMatches = Matches(cost[0, 1]) + Matches(cost[1, 0]);
                bool crossed;
                if (straightMatches == 0 && crossMatches == 0)
                {
                    // Both hands are new: follow the labels.
                    int first = PreferredSlot(hands[0].Side, 0);
                    if (hands[0].Side == HandSide.Unknown && hands[1].Side != HandSide.Unknown)
                        first = 1 - PreferredSlot(hands[1].Side, 1);
                    crossed = first == 1;
                }
                else if (straightMatches != crossMatches)
                {
                    crossed = crossMatches > straightMatches;
                }
                else
                {
                    crossed = Sum(cost[0, 1], cost[1, 0]) < Sum(cost[0, 0], cost[1, 1]);
                }
                slots[0] = crossed ? 1 : 0;
                slots[1] = crossed ? 0 : 1;
            }

            for (int h = 0; h < n; h++)
            {
                _centre[slots[h]] = centres[h];
                _seen[slots[h]] = now;
            }
            return slots;
        }

        private static int PreferredSlot(HandSide side, int fallback)
        {
            return side == HandSide.Left ? 0 : side == HandSide.Right ? 1 : fallback;
        }

        private static int Matches(float cost)
        {
            return float.IsInfinity(cost) ? 0 : 1;
        }

        private static float Sum(float a, float b)
        {
            return (float.IsInfinity(a) ? 0f : a) + (float.IsInfinity(b) ? 0f : b);
        }
    }
}
