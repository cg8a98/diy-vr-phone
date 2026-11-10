using DiyVr.Hands;
using NUnit.Framework;

namespace DiyVr.Tests.Hands
{
    public class HandGpuGuardTests
    {
        [Test]
        public void FirstRun_TriesGpu()
        {
            bool blocked;
            Assert.IsTrue(HandGpuGuard.ShouldTryGpu(false, false, out blocked));
            Assert.IsFalse(blocked);
        }

        [Test]
        public void PendingFromLastRun_BlocksGpuForGood()
        {
            bool blocked;
            Assert.IsFalse(HandGpuGuard.ShouldTryGpu(true, false, out blocked));
            Assert.IsTrue(blocked);
        }

        [Test]
        public void AlreadyBlocked_StaysBlocked()
        {
            bool blocked;
            Assert.IsFalse(HandGpuGuard.ShouldTryGpu(false, true, out blocked));
            Assert.IsTrue(blocked);
            Assert.IsFalse(HandGpuGuard.ShouldTryGpu(true, true, out blocked));
            Assert.IsTrue(blocked);
        }
    }
}
