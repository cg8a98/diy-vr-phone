using DiyVr.Passthrough;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class PassthroughStateMachineTests
    {
        [Test]
        public void StartsOff()
        {
            var s = new PassthroughStateMachine();
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
            Assert.IsFalse(s.IsVisible);
        }

        [Test]
        public void Toggle_OffToManualOn_AndBack()
        {
            var s = new PassthroughStateMachine();
            s.Toggle();
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
            s.Toggle();
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
        }

        [Test]
        public void TrackingLost_FromOff_GoesAuto()
        {
            var s = new PassthroughStateMachine();
            s.SetTracking(false);
            Assert.AreEqual(PassthroughMode.AutoOnTrackingLost, s.Mode);
            Assert.IsTrue(s.IsVisible);
        }

        [Test]
        public void TrackingRecovered_FromAuto_GoesOff()
        {
            var s = new PassthroughStateMachine();
            s.SetTracking(false);
            s.SetTracking(true);
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
        }

        [Test]
        public void TrackingLoss_WhileManualOn_StaysManual_AndStaysAfterRecovery()
        {
            var s = new PassthroughStateMachine();
            s.Toggle();
            s.SetTracking(false);
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
            s.SetTracking(true);
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
        }

        [Test]
        public void Toggle_FromAuto_PinsManualOn_SoRecoveryDoesNotHide()
        {
            var s = new PassthroughStateMachine();
            s.SetTracking(false);
            s.Toggle();
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
            s.SetTracking(true);
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
        }

        [Test]
        public void Toggle_OffFromManual_WhileTrackingLost_FallsBackToAuto()
        {
            var s = new PassthroughStateMachine();
            s.Toggle();
            s.SetTracking(false);
            s.Toggle();
            Assert.AreEqual(PassthroughMode.AutoOnTrackingLost, s.Mode);
            s.SetTracking(true);
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
        }

        [Test]
        public void TrackingFlaps_WhileOff_FollowTracking()
        {
            var s = new PassthroughStateMachine();
            s.SetTracking(false);
            s.SetTracking(true);
            s.SetTracking(false);
            Assert.AreEqual(PassthroughMode.AutoOnTrackingLost, s.Mode);
            s.SetTracking(false); // repeated loss is harmless
            Assert.AreEqual(PassthroughMode.AutoOnTrackingLost, s.Mode);
        }

        [Test]
        public void RepeatedRecovery_WhileOff_DoesNothing()
        {
            var s = new PassthroughStateMachine();
            s.SetTracking(true);
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
        }

        [Test]
        public void SetManual_OnAndOff()
        {
            var s = new PassthroughStateMachine();
            s.SetManual(false);
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
            s.SetManual(true);
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
            s.SetManual(true);
            Assert.AreEqual(PassthroughMode.ManualOn, s.Mode);
            s.SetManual(false);
            Assert.AreEqual(PassthroughMode.Off, s.Mode);
        }

        [Test]
        public void SetManualOff_DuringAuto_KeepsAuto()
        {
            var s = new PassthroughStateMachine();
            s.SetTracking(false);
            s.SetManual(false);
            Assert.AreEqual(PassthroughMode.AutoOnTrackingLost, s.Mode);
        }

        [Test]
        public void ModeChanged_FiresOnlyOnRealChanges()
        {
            var s = new PassthroughStateMachine();
            int n = 0;
            s.ModeChanged += m => n++;
            s.SetTracking(true);   // no change
            s.Toggle();            // 1: Off -> Manual
            s.SetTracking(false);  // no change (manual)
            s.Toggle();            // 2: Manual -> Auto
            s.SetTracking(true);   // 3: Auto -> Off
            Assert.AreEqual(3, n);
        }
    }
}
