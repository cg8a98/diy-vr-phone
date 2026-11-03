using DiyVr.Passthrough;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class PassthroughCullingTests
    {
        private const int AllButComposite = ~(1 << 31);

        [Test]
        public void QuadLayers_AreDistinctAndNotCompositeLayer()
        {
            Assert.AreNotEqual(PassthroughLayout.LeftLayer, PassthroughLayout.RightLayer);
            Assert.AreNotEqual(31, PassthroughLayout.LeftLayer);
            Assert.AreNotEqual(31, PassthroughLayout.RightLayer);
        }

        [Test]
        public void LeftEye_SeesOwnQuad_NotRightQuad()
        {
            int mask = PassthroughLayout.EyeCullingMask(AllButComposite, true);
            Assert.AreNotEqual(0, mask & (1 << PassthroughLayout.LeftLayer));
            Assert.AreEqual(0, mask & (1 << PassthroughLayout.RightLayer));
        }

        [Test]
        public void RightEye_SeesOwnQuad_NotLeftQuad()
        {
            int mask = PassthroughLayout.EyeCullingMask(AllButComposite, false);
            Assert.AreNotEqual(0, mask & (1 << PassthroughLayout.RightLayer));
            Assert.AreEqual(0, mask & (1 << PassthroughLayout.LeftLayer));
        }

        [Test]
        public void OtherLayersAreKept_AndCompositeStaysExcluded()
        {
            int mask = PassthroughLayout.EyeCullingMask(AllButComposite, true);
            Assert.AreNotEqual(0, mask & 1);          // Default layer still drawn
            Assert.AreEqual(0, mask & (1 << 31));     // compositor layer still excluded
        }

        [Test]
        public void Applying_IsIdempotent()
        {
            int once = PassthroughLayout.EyeCullingMask(AllButComposite, false);
            Assert.AreEqual(once, PassthroughLayout.EyeCullingMask(once, false));
        }
    }
}
