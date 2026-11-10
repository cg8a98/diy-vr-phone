using DiyVr.Room;
using NUnit.Framework;

namespace DiyVr.Tests
{
    public class ScanLoadLogicTests
    {
        [TestCase(8192, 8192, 2048, 2048, 2048)]
        [TestCase(8192, 4096, 2048, 2048, 1024)]
        [TestCase(4096, 8192, 2048, 1024, 2048)]
        [TestCase(2048, 2048, 2048, 2048, 2048)] // exactly at the limit: untouched
        [TestCase(1024, 512, 2048, 1024, 512)]   // small textures are never made bigger
        [TestCase(2049, 100, 2048, 2048, 99)]
        [TestCase(16384, 1, 2048, 2048, 1)]      // never collapses to zero
        [TestCase(0, 0, 2048, 1, 1)]
        public void TextureTarget_CutsLongSide_KeepsShape(int w, int h, int max, int expectedW, int expectedH)
        {
            int outW, outH;
            ScanLoadLogic.TextureTarget(w, h, max, out outW, out outH);
            Assert.AreEqual(expectedW, outW, "width");
            Assert.AreEqual(expectedH, outH, "height");
        }

        [Test]
        public void MaxTextureSize_Is2048()
        {
            Assert.AreEqual(2048, ScanLoadLogic.MaxTextureSize);
            Assert.IsTrue(ScanLoadLogic.NeedsDownscale(8192, 8192, ScanLoadLogic.MaxTextureSize));
            Assert.IsTrue(ScanLoadLogic.NeedsDownscale(100, 2049, ScanLoadLogic.MaxTextureSize));
            Assert.IsFalse(ScanLoadLogic.NeedsDownscale(2048, 2048, ScanLoadLogic.MaxTextureSize));
        }

        [TestCase("bedroom.glb", "bedroom.glb")]
        [TestCase("Bedroom.GLB", "Bedroom.glb")]
        [TestCase("bedroom", "bedroom.glb")]
        [TestCase("my room (2).glb", "my room _2_.glb")]
        [TestCase("../../etc/passwd", "passwd.glb")]
        [TestCase("C:\\private\\bedroom.glb", "bedroom.glb")]
        [TestCase("primary:Download/bedroom.glb", "bedroom.glb")]
        [TestCase("", "scan.glb")]
        [TestCase(null, "scan.glb")]
        [TestCase("...", "scan.glb")]
        [TestCase(".glb", "scan.glb")]
        public void SafeFileName_IsAPlainGlbName(string picked, string expected)
        {
            Assert.AreEqual(expected, ScanLoadLogic.SafeFileName(picked));
        }

        [Test]
        public void SafeFileName_IsCutToALimit()
        {
            string name = ScanLoadLogic.SafeFileName(new string('a', 500) + ".glb");
            Assert.AreEqual(ScanLoadLogic.MaxNameLength + ScanLoadLogic.Extension.Length, name.Length);
            StringAssert.EndsWith(".glb", name);
        }

        [Test]
        public void LooksLikeGlb_NeedsMagicAndVersion2()
        {
            byte[] good = { (byte)'g', (byte)'l', (byte)'T', (byte)'F', 2, 0, 0, 0, 12, 0, 0, 0 };
            Assert.IsTrue(ScanLoadLogic.LooksLikeGlb(good, good.Length));

            byte[] version1 = (byte[])good.Clone();
            version1[4] = 1;
            Assert.IsFalse(ScanLoadLogic.LooksLikeGlb(version1, version1.Length), "version 1");

            byte[] zip = { (byte)'P', (byte)'K', 3, 4, 2, 0, 0, 0, 12, 0, 0, 0 };
            Assert.IsFalse(ScanLoadLogic.LooksLikeGlb(zip, zip.Length), "another file type");

            Assert.IsFalse(ScanLoadLogic.LooksLikeGlb(good, 8), "too short");
            Assert.IsFalse(ScanLoadLogic.LooksLikeGlb(null, 0), "null");
            Assert.IsFalse(ScanLoadLogic.LooksLikeGlb(new byte[0], 0), "empty");
        }

        [Test]
        public void StatusText_ShowsNameTrianglesAndTextureSize()
        {
            Assert.AreEqual("Scan: bedroom, 104212 tris, 2048px", ScanLoadLogic.StatusText("bedroom", 104212, 2048));
            Assert.AreEqual("Scan: not a .glb file", ScanLoadLogic.ErrorText("not a .glb file"));
            Assert.AreEqual("Scan: unknown error", ScanLoadLogic.ErrorText(null));
        }

        [Test]
        public void Shorten_KeepsOneShortLine()
        {
            Assert.AreEqual("abc", ScanLoadLogic.Shorten("abc", 10));
            Assert.AreEqual("abcdefg...", ScanLoadLogic.Shorten("abcdefghijklmnop", 10));
            Assert.AreEqual("a b", ScanLoadLogic.Shorten("a\nb", 10));
            Assert.AreEqual("", ScanLoadLogic.Shorten(null, 10));
        }

        [Test]
        public void Button_QuickPress_FiresShortPressOnRelease()
        {
            var b = new ScanButtonLogic();
            Assert.AreEqual(ScanButtonEvent.None, b.Update(false, 0.016f));
            Assert.AreEqual(ScanButtonEvent.None, b.Update(true, 0.016f), "nothing while going down");
            Assert.AreEqual(ScanButtonEvent.None, b.Update(true, 0.1f));
            Assert.AreEqual(ScanButtonEvent.ShortPress, b.Update(false, 0.016f));
            Assert.AreEqual(ScanButtonEvent.None, b.Update(false, 0.016f), "fires once");
        }

        [Test]
        public void Button_Hold_FiresLongPressOnce_AndNoShortPressAfter()
        {
            var b = new ScanButtonLogic();
            b.Update(true, 0f);
            int longPresses = 0;
            for (int i = 0; i < 200; i++)
                if (b.Update(true, 0.016f) == ScanButtonEvent.LongPress) longPresses++;
            Assert.AreEqual(1, longPresses, "long press fires once while held");
            Assert.AreEqual(ScanButtonEvent.None, b.Update(false, 0.016f), "letting go after a long press does nothing");

            // The next quick press works again.
            b.Update(true, 0.016f);
            Assert.AreEqual(ScanButtonEvent.ShortPress, b.Update(false, 0.016f));
        }

        [Test]
        public void Button_LongPress_NeedsTheFullHoldTime()
        {
            var b = new ScanButtonLogic();
            b.Update(true, 0f);
            Assert.AreEqual(ScanButtonEvent.None, b.Update(true, ScanButtonLogic.LongPressSeconds * 0.9f));
            Assert.AreEqual(ScanButtonEvent.LongPress, b.Update(true, ScanButtonLogic.LongPressSeconds * 0.2f));
        }
    }
}
