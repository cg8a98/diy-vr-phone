using System.Collections.Generic;
using DiyVr.App;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class GamepadMapperTests
    {
        private GamepadMapper _m;
        private List<GamepadAction> _out;

        [SetUp]
        public void SetUp() { _m = new GamepadMapper(); _out = new List<GamepadAction>(); }

        private List<GamepadAction> Step(GamepadButtons b, bool lensOpen, float dt = 0.016f)
        {
            _out.Clear();
            _m.Update(b, lensOpen, dt, _out);
            return _out;
        }

        private static GamepadButtons None { get { return new GamepadButtons(); } }

        [Test]
        public void YTogglesLensTool_OpenOrClosed()
        {
            Assert.That(Step(new GamepadButtons { Y = true }, false), Is.EqualTo(new[] { GamepadAction.ToggleLensTool }));
            Step(None, false);
            Assert.That(Step(new GamepadButtons { Y = true }, true), Is.EqualTo(new[] { GamepadAction.ToggleLensTool }));
        }

        [Test]
        public void EachLensButtonMapsToItsAction()
        {
            Assert.That(Step(new GamepadButtons { DpadUp = true }, true), Is.EqualTo(new[] { GamepadAction.SelectPrevious })); Step(None, true);
            Assert.That(Step(new GamepadButtons { DpadDown = true }, true), Is.EqualTo(new[] { GamepadAction.SelectNext })); Step(None, true);
            Assert.That(Step(new GamepadButtons { DpadLeft = true }, true), Is.EqualTo(new[] { GamepadAction.AdjustDown })); Step(None, true);
            Assert.That(Step(new GamepadButtons { DpadRight = true }, true), Is.EqualTo(new[] { GamepadAction.AdjustUp })); Step(None, true);
            Assert.That(Step(new GamepadButtons { LeftShoulder = true }, true), Is.EqualTo(new[] { GamepadAction.AdjustDownBig })); Step(None, true);
            Assert.That(Step(new GamepadButtons { RightShoulder = true }, true), Is.EqualTo(new[] { GamepadAction.AdjustUpBig })); Step(None, true);
            Assert.That(Step(new GamepadButtons { A = true }, true), Is.EqualTo(new[] { GamepadAction.Save })); Step(None, true);
            Assert.That(Step(new GamepadButtons { B = true }, true), Is.EqualTo(new[] { GamepadAction.CloseLensTool }));
        }

        [Test]
        public void XAndView_Map()
        {
            Assert.That(Step(new GamepadButtons { X = true }, false), Is.EqualTo(new[] { GamepadAction.TogglePassthrough }));
            Assert.That(Step(new GamepadButtons { View = true }, false), Is.EqualTo(new[] { GamepadAction.ToggleOverlay }));
        }

        [Test]
        public void XAndView_WorkWhileLensToolOpen()
        {
            Assert.That(Step(new GamepadButtons { X = true }, true), Is.EqualTo(new[] { GamepadAction.TogglePassthrough }));
            Assert.That(Step(new GamepadButtons { View = true }, true), Is.EqualTo(new[] { GamepadAction.ToggleOverlay }));
        }

        [Test]
        public void PressFiresOnce_NotEveryFrame()
        {
            var b = new GamepadButtons { A = true, X = true, DpadUp = true };
            Assert.That(Step(b, true).Count, Is.EqualTo(3));
            Assert.That(Step(b, true).Count, Is.EqualTo(0));
            Assert.That(Step(b, true).Count, Is.EqualTo(0));
            Step(None, true);
            Assert.That(Step(b, true).Count, Is.EqualTo(3));
        }

        [Test]
        public void LensOnlyActions_IgnoredWhenClosed()
        {
            var all = new GamepadButtons { B = true, DpadUp = true, DpadDown = true, DpadLeft = true, LeftShoulder = true, RightShoulder = true };
            Assert.That(Step(all, false), Is.Empty);
            Step(None, false);
            Assert.That(Step(new GamepadButtons { DpadRight = true }, false), Is.Empty);
        }

        [Test]
        public void A_WhenClosed_PlacesCube_OnceNotEveryFrame()
        {
            var a = new GamepadButtons { A = true };
            Assert.That(Step(a, false), Is.EqualTo(new[] { GamepadAction.PlaceCube }));
            Assert.That(Step(a, false), Is.Empty);
            Step(None, false);
            Assert.That(Step(a, false), Is.EqualTo(new[] { GamepadAction.PlaceCube }));
        }

        [Test]
        public void A_WhenOpen_StillSaves_NotPlaceCube()
        {
            Assert.That(Step(new GamepadButtons { A = true }, true), Is.EqualTo(new[] { GamepadAction.Save }));
        }

        [Test]
        public void A_HeldWhileLensToolCloses_DoesNotPlaceCube()
        {
            Step(new GamepadButtons { A = true }, true);
            Assert.That(Step(new GamepadButtons { A = true }, false), Is.Empty);
        }

        [Test]
        public void ButtonHeldBeforeLensOpens_DoesNotFire()
        {
            Step(new GamepadButtons { A = true }, false);
            Assert.That(Step(new GamepadButtons { A = true }, true), Is.Empty);
        }

        [Test]
        public void HeldDpadRight_RepeatsAfterDelayThenEveryInterval()
        {
            var right = new GamepadButtons { DpadRight = true };
            Assert.That(Step(right, true, 0.016f).Count, Is.EqualTo(1));          // the press itself
            Assert.That(Step(right, true, 0.30f).Count, Is.EqualTo(0));           // held 0.30 s
            Assert.That(Step(right, true, 0.09f).Count, Is.EqualTo(0));           // 0.39 s, still waiting
            Assert.That(Step(right, true, 0.02f), Is.EqualTo(new[] { GamepadAction.AdjustUp }));   // 0.41 s: first repeat
            Assert.That(Step(right, true, 0.05f).Count, Is.EqualTo(0));           // 0.46 s
            Assert.That(Step(right, true, 0.04f).Count, Is.EqualTo(1));           // 0.50 s: 0.48 reached
            Assert.That(Step(right, true, 0.16f).Count, Is.EqualTo(2));           // 0.66 s: 0.56 and 0.64
        }

        [Test]
        public void HeldDpadLeft_RepeatsAdjustDown_AndReleaseResets()
        {
            var left = new GamepadButtons { DpadLeft = true };
            Step(left, true);
            Assert.That(Step(left, true, 0.41f), Is.EqualTo(new[] { GamepadAction.AdjustDown }));
            Step(None, true);
            Assert.That(Step(left, true), Is.EqualTo(new[] { GamepadAction.AdjustDown }));   // new press
            Assert.That(Step(left, true, 0.2f).Count, Is.EqualTo(0));                        // delay restarted
        }

        [Test]
        public void ShouldersAndOtherButtons_DoNotRepeat()
        {
            var b = new GamepadButtons { LeftShoulder = true, DpadUp = true, A = true };
            Step(b, true);
            Assert.That(Step(b, true, 1f), Is.Empty);
        }

        [Test]
        public void ClosingLensTool_StopsRepeat()
        {
            var right = new GamepadButtons { DpadRight = true };
            Step(right, true);
            Step(right, false, 0.5f);
            Assert.That(Step(right, true, 0.5f), Is.Empty);
        }

        [Test]
        public void RightStick_TogglesPlanes_OpenOrClosed_EdgeOnly()
        {
            var b = new GamepadButtons { RightStick = true };
            Assert.That(Step(b, false), Is.EqualTo(new[] { GamepadAction.TogglePlanes }));
            Assert.That(Step(b, false), Is.Empty);
            Step(None, false);
            Assert.That(Step(b, true), Is.EqualTo(new[] { GamepadAction.TogglePlanes }));
        }
    }
}
