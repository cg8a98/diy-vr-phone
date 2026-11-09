using System.Collections.Generic;
using DiyVr.Updater;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class UpdateLogicTests
    {
        private const string Url = UpdateLogic.DownloadPrefix + "v0.1.81/diy-vr-0.1.81.apk";

        private static string Json(string tag, string assetName = "diy-vr-0.1.81.apk", string url = Url, long size = 123456)
        {
            return "{\"id\":1,\"tag_name\":\"" + tag + "\",\"name\":\"DIY VR\",\"assets\":[" +
                   "{\"name\":\"notes.txt\",\"size\":5,\"browser_download_url\":\"" + UpdateLogic.DownloadPrefix + "x/notes.txt\"}," +
                   "{\"name\":\"" + assetName + "\",\"size\":" + size + ",\"browser_download_url\":\"" + url + "\"}]}";
        }

        [TestCase("v0.1.80", true)]
        [TestCase("0.1.80", true)]
        [TestCase("V0.1.80", true)]
        [TestCase(" v0.1.80 ", true)]
        [TestCase("1.0", true)]
        [TestCase("0.1.80.2", true)]
        [TestCase("v0.1", true)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("latest", false)]
        [TestCase("v1", false)]
        [TestCase("0.1.x", false)]
        [TestCase("0.1.-3", false)]
        [TestCase("0.1.80-beta", false)]
        [TestCase("0..1", false)]
        [TestCase("1.2.3.4.5", false)]
        [TestCase("0.1.99999999999", false)]
        public void TryParse(string text, bool ok)
        {
            ReleaseVersion v;
            Assert.AreEqual(ok, ReleaseVersion.TryParse(text, out v));
        }

        [Test]
        public void ToString_RoundTrips()
        {
            ReleaseVersion v;
            Assert.IsTrue(ReleaseVersion.TryParse("v0.1.80", out v));
            Assert.AreEqual("0.1.80", v.ToString());
        }

        [TestCase("0.1.80", "v0.1.81", true)]
        [TestCase("0.1.80", "0.1.80", false)]
        [TestCase("0.1.81", "v0.1.80", false)]
        [TestCase("0.1.9", "v0.1.10", true)]      // numeric, not text, compare
        [TestCase("0.1.99", "v0.2.0", true)]
        [TestCase("0.1.80", "v1.0.0", true)]
        [TestCase("0.1.0", "v0.1.1", true)]
        [TestCase("0.1.80", "nonsense", false)]
        [TestCase("dev", "v0.1.81", false)]
        [TestCase("", "v0.1.81", false)]
        public void IsNewer(string current, string latest, bool expected)
        {
            Assert.AreEqual(expected, UpdateLogic.IsNewer(current, latest));
        }

        [Test]
        public void TryGetOffer_NewerRelease_PicksApk()
        {
            var offer = UpdateLogic.TryGetOffer(Json("v0.1.81"), "0.1.80");
            Assert.IsNotNull(offer);
            Assert.AreEqual("0.1.81", offer.Version);
            Assert.AreEqual(Url, offer.Url);
            Assert.AreEqual(123456L, offer.Size);
        }

        [Test]
        public void TryGetOffer_SameOrOlder_ReturnsNull()
        {
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("v0.1.80"), "0.1.80"));
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("v0.1.79"), "0.1.80"));
        }

        [Test]
        public void TryGetOffer_BadTag_ReturnsNull()
        {
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("nightly"), "0.1.80"));
        }

        [Test]
        public void TryGetOffer_NoApk_ReturnsNull()
        {
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("v0.1.81", assetName: "diy-vr.zip"), "0.1.80"));
        }

        [Test]
        public void TryGetOffer_ApkFromOtherHost_ReturnsNull()
        {
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("v0.1.81", url: "https://example.com/diy-vr-0.1.81.apk"), "0.1.80"));
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("v0.1.81", url: "http://github.com/cg8a98/diy-vr-phone/releases/download/v0.1.81/a.apk"), "0.1.80"));
        }

        [Test]
        public void TryGetOffer_ZeroSize_ReturnsNull()
        {
            Assert.IsNull(UpdateLogic.TryGetOffer(Json("v0.1.81", size: 0), "0.1.80"));
        }

        [Test]
        public void TryGetOffer_GarbageJson_ReturnsNull()
        {
            Assert.IsNull(UpdateLogic.TryGetOffer("not json", "0.1.80"));
            Assert.IsNull(UpdateLogic.TryGetOffer("", "0.1.80"));
            Assert.IsNull(UpdateLogic.TryGetOffer("{\"message\":\"Not Found\"}", "0.1.80"));
        }

        [Test]
        public void PickApk_SkipsNonApkAssets()
        {
            var r = new ReleaseDto { tag_name = "v0.1.81", assets = new List<ReleaseAssetDto> {
                new ReleaseAssetDto { name = "a.zip", size = 1, browser_download_url = UpdateLogic.DownloadPrefix + "a.zip" },
                new ReleaseAssetDto { name = "B.APK", size = 9, browser_download_url = UpdateLogic.DownloadPrefix + "B.APK" } } };
            Assert.AreEqual("B.APK", UpdateLogic.PickApk(r).name);
            Assert.IsNull(UpdateLogic.PickApk(null));
            Assert.IsNull(UpdateLogic.PickApk(new ReleaseDto()));
        }

        [Test]
        public void OfferText_HasVersionAndButtons()
        {
            StringAssert.Contains("Update v0.1.81 available", UpdateLogic.OfferText("0.1.81"));
            StringAssert.Contains("A: install, B: later", UpdateLogic.OfferText("0.1.81"));
        }
    }
}
