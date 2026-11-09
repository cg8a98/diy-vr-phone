using DiyVr.Interaction;
using NUnit.Framework;

namespace DiyVr.Tests.Interaction
{
    public class GazeMathTests
    {
        [Test]
        public void ReticleScale_OneDegreeAtOneMetre_IsAbout17mm()
        {
            Assert.AreEqual(0.017453f, GazeMath.ReticleScale(1f, 1f), 1e-5f);
        }

        [Test]
        public void ReticleScale_GrowsLinearlyWithDistance()
        {
            Assert.AreEqual(GazeMath.ReticleScale(1f, 1f) * 4f, GazeMath.ReticleScale(4f, 1f), 1e-5f);
        }

        [Test]
        public void ReticleScale_BadInput_IsZero()
        {
            Assert.AreEqual(0f, GazeMath.ReticleScale(0f, 1f));
            Assert.AreEqual(0f, GazeMath.ReticleScale(-1f, 1f));
            Assert.AreEqual(0f, GazeMath.ReticleScale(1f, 0f));
        }

        [Test]
        public void ReticleDistance_UsesHitOrMissDistance()
        {
            Assert.AreEqual(0.7f, GazeMath.ReticleDistance(true, 0.7f, 2f));
            Assert.AreEqual(2f, GazeMath.ReticleDistance(false, 0f, 2f));
            Assert.AreEqual(2f, GazeMath.ReticleDistance(true, 0f, 2f));
        }

        [Test]
        public void Switch_FirstTarget_Enters()
        {
            var s = new GazeTargetSwitch<string>();
            string exited, entered;
            Assert.IsTrue(s.Update("a", out exited, out entered));
            Assert.IsNull(exited);
            Assert.AreEqual("a", entered);
            Assert.AreEqual("a", s.Current);
        }

        [Test]
        public void Switch_SameTarget_NoChange()
        {
            var s = new GazeTargetSwitch<string>();
            string exited, entered;
            s.Update("a", out exited, out entered);
            Assert.IsFalse(s.Update("a", out exited, out entered));
            Assert.IsNull(exited);
            Assert.IsNull(entered);
        }

        [Test]
        public void Switch_AToB_ExitsAThenEntersB()
        {
            var s = new GazeTargetSwitch<string>();
            string exited, entered;
            s.Update("a", out exited, out entered);
            Assert.IsTrue(s.Update("b", out exited, out entered));
            Assert.AreEqual("a", exited);
            Assert.AreEqual("b", entered);
        }

        [Test]
        public void Switch_ToNull_ExitsOnly()
        {
            var s = new GazeTargetSwitch<string>();
            string exited, entered;
            s.Update("a", out exited, out entered);
            Assert.IsTrue(s.Update(null, out exited, out entered));
            Assert.AreEqual("a", exited);
            Assert.IsNull(entered);
            Assert.IsNull(s.Current);
        }

        [Test]
        public void Switch_NullToNull_NoChange()
        {
            var s = new GazeTargetSwitch<string>();
            string exited, entered;
            Assert.IsFalse(s.Update(null, out exited, out entered));
        }
    }
}
