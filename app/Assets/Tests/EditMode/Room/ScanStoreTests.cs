using System;
using System.IO;
using DiyVr.Room;
using NUnit.Framework;

namespace DiyVr.Tests
{
    /// <summary>File copy decisions and the "last scan" memory, in a throw-away temp folder.</summary>
    public class ScanStoreTests
    {
        private string _root;
        private ScanStore _store;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "diyvr-scan-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _store = new ScanStore(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private void WriteIncoming(byte[] bytes)
        {
            _store.PrepareIncoming();
            File.WriteAllBytes(_store.IncomingPath, bytes);
        }

        [Test]
        public void NothingImportedYet_NoLastScan()
        {
            Assert.IsNull(_store.LastScanPath());
            StringAssert.StartsWith(_root, _store.DirectoryPath, "scans live inside the app's own folder");
            StringAssert.EndsWith("scans", _store.DirectoryPath);
        }

        [Test]
        public void Import_KeepsTheFileUnderASafeName_AndRemembersIt()
        {
            byte[] glb = SyntheticGlb.Build(false);
            WriteIncoming(glb);

            string path, error;
            Assert.IsTrue(_store.TryImport("my bedroom!.glb", out path, out error), error);
            Assert.AreEqual(Path.Combine(_store.DirectoryPath, "my bedroom_.glb"), path);
            Assert.AreEqual(glb, File.ReadAllBytes(path));
            Assert.IsFalse(File.Exists(_store.IncomingPath), "the temp copy is moved, not left behind");
            Assert.AreEqual("my bedroom_", ScanStore.DisplayName(path));

            // A fresh store (next app start) finds it again.
            Assert.AreEqual(path, new ScanStore(_root).LastScanPath());
        }

        [Test]
        public void Import_SameNameAgain_ReplacesTheOldFile()
        {
            string path, error;
            WriteIncoming(SyntheticGlb.Build(false));
            Assert.IsTrue(_store.TryImport("room.glb", out path, out error), error);
            byte[] second = SyntheticGlb.Build(true);
            WriteIncoming(second);
            Assert.IsTrue(_store.TryImport("room.glb", out path, out error), error);
            Assert.AreEqual(second, File.ReadAllBytes(path));
            Assert.AreEqual(1, Directory.GetFiles(_store.DirectoryPath, "*.glb").Length);
        }

        [Test]
        public void Import_SecondScan_BecomesTheLastOne()
        {
            string first, second, error;
            WriteIncoming(SyntheticGlb.Build(false));
            Assert.IsTrue(_store.TryImport("bedroom.glb", out first, out error), error);
            WriteIncoming(SyntheticGlb.Build(false));
            Assert.IsTrue(_store.TryImport("kitchen.glb", out second, out error), error);
            Assert.AreEqual(second, _store.LastScanPath());
            Assert.IsTrue(File.Exists(first), "the earlier scan is kept");
        }

        [Test]
        public void Import_NotAGlb_IsRejectedAndDeleted_AndTheLastScanStays()
        {
            string good, path, error;
            WriteIncoming(SyntheticGlb.Build(false));
            Assert.IsTrue(_store.TryImport("bedroom.glb", out good, out error), error);

            WriteIncoming(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 });
            Assert.IsFalse(_store.TryImport("holiday.jpg", out path, out error));
            Assert.IsNull(path);
            Assert.AreEqual("not a .glb file", error);
            Assert.IsFalse(File.Exists(_store.IncomingPath), "the rejected file is deleted");
            Assert.AreEqual(good, _store.LastScanPath());
        }

        [Test]
        public void Import_NothingCopied_ReportsIt()
        {
            _store.PrepareIncoming();
            string path, error;
            Assert.IsFalse(_store.TryImport("bedroom.glb", out path, out error));
            Assert.AreEqual("no file was copied", error);
        }

        [Test]
        public void PrepareIncoming_RemovesAHalfCopiedFile()
        {
            WriteIncoming(new byte[] { 1, 2, 3 });
            _store.PrepareIncoming();
            Assert.IsFalse(File.Exists(_store.IncomingPath));
        }

        [Test]
        public void LastScan_FileDeleted_GivesNull()
        {
            string path, error;
            WriteIncoming(SyntheticGlb.Build(false));
            Assert.IsTrue(_store.TryImport("bedroom.glb", out path, out error), error);
            File.Delete(path);
            Assert.IsNull(_store.LastScanPath());
        }

        [Test]
        public void LastScan_MarkerCannotPointOutsideTheScanFolder()
        {
            string outside = Path.Combine(_root, "secret.glb");
            File.WriteAllBytes(outside, SyntheticGlb.Build(false));
            Directory.CreateDirectory(_store.DirectoryPath);
            File.WriteAllText(Path.Combine(_store.DirectoryPath, ScanStore.LastFileName), "../secret.glb");
            Assert.IsNull(_store.LastScanPath());
        }
    }
}
