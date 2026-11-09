using DiyVr.Tracking;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class PlaneClassifierTests
    {
        [Test] public void LowestHeight_Empty_IsZero() { Assert.AreEqual(0f, PlaneClassifier.LowestHeight(new float[0])); }
        [Test] public void LowestHeight_PicksMinimum() { Assert.AreEqual(-1.4f, PlaneClassifier.LowestHeight(new[] { 0.3f, -1.4f, -0.7f }), 1e-6f); }
        [Test] public void LowestPlane_IsFloor() { Assert.AreEqual(PlaneKind.Floor, PlaneClassifier.Classify(-1.4f, -1.4f)); }
        [Test] public void NearLowest_IsFloor() { Assert.AreEqual(PlaneKind.Floor, PlaneClassifier.Classify(-1.3f, -1.4f)); }
        [Test] public void DeskHeight_IsDesk() { Assert.AreEqual(PlaneKind.Desk, PlaneClassifier.Classify(-0.65f, -1.4f)); }
        [Test] public void BetweenFloorAndDesk_IsOther() { Assert.AreEqual(PlaneKind.Other, PlaneClassifier.Classify(-1.1f, -1.4f)); }
        [Test] public void TooHigh_IsOther() { Assert.AreEqual(PlaneKind.Other, PlaneClassifier.Classify(0.5f, -1.4f)); }
        [Test]
        public void DeskBounds_Inclusive()
        {
            Assert.AreEqual(PlaneKind.Desk, PlaneClassifier.Classify(PlaneClassifier.DeskMin, 0f));
            Assert.AreEqual(PlaneKind.Desk, PlaneClassifier.Classify(PlaneClassifier.DeskMax, 0f));
        }
        [Test] public void LivePlane_TrackingNotMerged_IsLive() { Assert.IsTrue(PlaneClassifier.IsLivePlane(false, true, true)); }
        [Test] public void LivePlane_Subsumed_IsNotLive() { Assert.IsFalse(PlaneClassifier.IsLivePlane(true, true, true)); }
        [Test] public void LivePlane_NotTracking_IsNotLive() { Assert.IsFalse(PlaneClassifier.IsLivePlane(false, false, true)); }
        [Test] public void LivePlane_NoTrackableId_IsNotLive() { Assert.IsFalse(PlaneClassifier.IsLivePlane(false, true, false)); }
        [Test] public void ShouldShow_ViewOff_IsHidden() { Assert.IsFalse(PlaneClassifier.ShouldShow(false, false, true, true)); }
        [Test] public void ShouldShow_ViewOnLivePlane_IsShown() { Assert.IsTrue(PlaneClassifier.ShouldShow(true, false, true, true)); }
        [Test] public void ShouldShow_ViewOnSubsumed_IsHidden() { Assert.IsFalse(PlaneClassifier.ShouldShow(true, true, true, true)); }
        [Test] public void ShouldShow_ViewOnTemplate_IsHidden() { Assert.IsFalse(PlaneClassifier.ShouldShow(true, false, true, false)); }
    }
}
