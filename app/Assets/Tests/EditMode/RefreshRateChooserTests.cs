using DiyVr.Core;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class RefreshRateChooserTests
    {
        [Test]
        public void PicksHighestAtMatchingSize()
        {
            var w = new[] { 2316, 2316, 1544, 1544 };
            var h = new[] { 1080, 1080, 720, 720 };
            var hz = new[] { 60.0, 120.0, 60.0, 120.0 };
            Assert.AreEqual(120.0, RefreshRateChooser.ChooseHighest(w, h, hz, 2316, 1080));
        }

        [Test]
        public void IgnoresOtherSizes()
        {
            var w = new[] { 2316, 1544 };
            var h = new[] { 1080, 720 };
            var hz = new[] { 60.0, 120.0 };
            Assert.AreEqual(60.0, RefreshRateChooser.ChooseHighest(w, h, hz, 2316, 1080));
        }

        [Test]
        public void NoExactMatchFallsBackToHighestOverall()
        {
            var w = new[] { 3088, 3088, 1544 };
            var h = new[] { 1440, 1440, 720 };
            var hz = new[] { 60.0, 120.0, 90.0 };
            Assert.AreEqual(120.0, RefreshRateChooser.ChooseHighest(w, h, hz, 2316, 1080));
        }

        [Test]
        public void ExactMatchWinsOverHigherOtherSize()
        {
            var w = new[] { 2316, 3088 };
            var h = new[] { 1080, 1440 };
            var hz = new[] { 60.0, 120.0 };
            Assert.AreEqual(60.0, RefreshRateChooser.ChooseHighest(w, h, hz, 2316, 1080));
        }

        [Test]
        public void EmptyArraysReturnZero()
        {
            Assert.AreEqual(0.0, RefreshRateChooser.ChooseHighest(new int[0], new int[0], new double[0], 200, 200));
        }

        [Test]
        public void BadInputReturnsZero()
        {
            Assert.AreEqual(0.0, RefreshRateChooser.ChooseHighest(null, null, null, 1, 1));
            Assert.AreEqual(0.0, RefreshRateChooser.ChooseHighest(new[] { 1, 2 }, new[] { 1 }, new[] { 60.0 }, 1, 1));
        }

        [Test]
        public void FractionalRatesCompareCorrectly()
        {
            var w = new[] { 10, 10 };
            var h = new[] { 10, 10 };
            var hz = new[] { 59.94, 60.0 };
            Assert.AreEqual(60.0, RefreshRateChooser.ChooseHighest(w, h, hz, 10, 10));
        }
    }
}
