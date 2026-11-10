using System;
using System.Collections.Generic;

namespace DiyVr.Experiments
{
    public enum TrialOutcome
    {
        Hit,    // pinched while gazing the target button
        Wrong,  // pinched while gazing another button
        Miss,   // pinched while gazing nothing
    }

    /// <summary>
    /// Pure logic of the five-button test: picks the target, scores each pinch, keeps totals and the last-10 window.
    /// The target only changes after a hit (so a wrong press does not move the goalposts); it never repeats the
    /// previous target. Seedable so tests are repeatable.
    /// </summary>
    public sealed class FiveButtonTrial
    {
        public const int WindowSize = 10;
        public const int PassHits = 9;

        private readonly int _buttonCount;
        private readonly Random _rng;
        private readonly Queue<TrialOutcome> _window = new Queue<TrialOutcome>();

        public int Target { get; private set; }
        public int Hits { get; private set; }
        public int Wrong { get; private set; }
        public int Misses { get; private set; }
        public int Pinches { get { return Hits + Wrong + Misses; } }

        public FiveButtonTrial(int buttonCount = 5, int seed = 0)
        {
            if (buttonCount < 2) throw new ArgumentOutOfRangeException("buttonCount", "need at least 2 buttons");
            _buttonCount = buttonCount;
            _rng = new Random(seed);
            Target = _rng.Next(_buttonCount);
        }

        /// <summary>Number of outcomes in the last-10 window (0..10).</summary>
        public int WindowCount { get { return _window.Count; } }

        /// <summary>Hits among the last 10 pinches.</summary>
        public int WindowHits
        {
            get
            {
                int n = 0;
                foreach (var o in _window) if (o == TrialOutcome.Hit) n++;
                return n;
            }
        }

        /// <summary>True when the last 10 pinches contain at least 9 hits (needs a full window of 10).</summary>
        public bool Passed { get { return _window.Count >= WindowSize && WindowHits >= PassHits; } }

        /// <summary>Score one pinch. gazedIndex is the button being looked at, or -1 for nothing / not a button.</summary>
        public TrialOutcome RecordPinch(int gazedIndex)
        {
            TrialOutcome outcome;
            if (gazedIndex < 0 || gazedIndex >= _buttonCount) outcome = TrialOutcome.Miss;
            else if (gazedIndex == Target) outcome = TrialOutcome.Hit;
            else outcome = TrialOutcome.Wrong;

            switch (outcome)
            {
                case TrialOutcome.Hit: Hits++; break;
                case TrialOutcome.Wrong: Wrong++; break;
                default: Misses++; break;
            }
            _window.Enqueue(outcome);
            while (_window.Count > WindowSize) _window.Dequeue();

            if (outcome == TrialOutcome.Hit) PickNextTarget();
            return outcome;
        }

        /// <summary>Zero all counts and pick a new target.</summary>
        public void Reset()
        {
            Hits = 0; Wrong = 0; Misses = 0;
            _window.Clear();
            Target = _rng.Next(_buttonCount);
        }

        /// <summary>"Hits 7 / Wrong 1 / Miss 2 - last 10: 8/10" (the second number is the window size so far, e.g. 3/3).</summary>
        public string Summary()
        {
            return "Hits " + Hits + " / Wrong " + Wrong + " / Miss " + Misses
                + " - last 10: " + WindowHits + "/" + _window.Count
                + (Passed ? "  PASS" : "");
        }

        private void PickNextTarget()
        {
            // Draw from the other buttons only, so the target never repeats.
            int next = _rng.Next(_buttonCount - 1);
            if (next >= Target) next++;
            Target = next;
        }
    }
}
