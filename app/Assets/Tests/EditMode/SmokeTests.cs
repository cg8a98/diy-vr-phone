using DiyVr.Core;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    public class SmokeTests
    {
        [Test]
        public void Bootstrap_RequestsHighestRefreshRate()
        {
            Assert.AreEqual(120, AppBootstrap.TargetFrameRate);
        }

        [Test]
        public void Bootstrap_KeepsScreenOn()
        {
            AppBootstrap.Apply();
            Assert.AreEqual(SleepTimeout.NeverSleep, Screen.sleepTimeout);
        }
    }
}
