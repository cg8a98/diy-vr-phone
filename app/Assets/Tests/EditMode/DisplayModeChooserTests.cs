using DiyVr.Core;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class DisplayModeChooserTests
    {
        private static readonly int[] Ids = { 1, 2, 3, 4 };
        private static readonly int[] W = { 3088, 3088, 2316, 2316 };
        private static readonly int[] H = { 1440, 1440, 1080, 1080 };

        [Test]
        public void PicksHighestAtCurrentSize()
        {
            var hz = new[] { 60f, 120f, 60f, 120f };
            Assert.AreEqual(3, DisplayModeChooser.ChooseIndex(Ids, W, H, hz, 2316, 1080));
        }

        [Test]
        public void SizeMatchBeatsHigherRateAtOtherSize()
        {
            var hz = new[] { 60f, 120f, 60f, 90f };
            Assert.AreEqual(3, DisplayModeChooser.ChooseIndex(Ids, W, H, hz, 2316, 1080));
        }

        [Test]
        public void FallsBackToHighestOverallWhenNoSizeMatches()
        {
            var hz = new[] { 60f, 120f, 60f, 90f };
            Assert.AreEqual(1, DisplayModeChooser.ChooseIndex(Ids, W, H, hz, 1000, 500));
        }

        [Test]
        public void TieKeepsFirst()
        {
            var hz = new[] { 120f, 120f, 60f, 60f };
            Assert.AreEqual(0, DisplayModeChooser.ChooseIndex(Ids, W, H, hz, 3088, 1440));
        }

        [Test]
        public void BadInputReturnsMinusOne()
        {
            Assert.AreEqual(-1, DisplayModeChooser.ChooseIndex(null, W, H, new float[4], 1, 1));
            Assert.AreEqual(-1, DisplayModeChooser.ChooseIndex(Ids, W, H, new float[3], 1, 1));
            Assert.AreEqual(-1, DisplayModeChooser.ChooseIndex(new int[0], new int[0], new int[0], new float[0], 1, 1));
        }
    }
}
