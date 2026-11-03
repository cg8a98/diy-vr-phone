using System.Collections.Generic;
using DiyVr.Stereo;
using DiyVr.Stereo.LensTool;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests
{
    internal sealed class MemoryFileStore : IFileStore
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();

        public bool Exists(string path) { return Files.ContainsKey(Norm(path)); }
        public string ReadAllText(string path) { return Files[Norm(path)]; }
        public void WriteAllText(string path, string text) { Files[Norm(path)] = text; }

        public IList<string> ListFileNames(string directory, string extension)
        {
            var result = new List<string>();
            var prefix = Norm(directory) + "/";
            foreach (var k in Files.Keys)
            {
                if (!k.StartsWith(prefix) || !k.EndsWith(extension)) continue;
                var rest = k.Substring(prefix.Length);
                if (rest.IndexOf('/') < 0) result.Add(rest);
            }
            return result;
        }

        private static string Norm(string p) { return p.Replace('\\', '/'); }
    }

    public class LensParameterTests
    {
        private LensProfile _p;

        [SetUp] public void SetUp() { _p = LensProfile.CreateDefault(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_p); }

        [Test]
        public void Ipd_StepsInHalfMillimetres()
        {
            var ipd = LensParameters.Get(LensParameter.Ipd);
            Assert.AreEqual(66.5, ipd.Get(_p), 1e-6);
            ipd.StepBy(_p, 1);
            Assert.AreEqual(67.0, ipd.Get(_p), 1e-6);
            Assert.AreEqual(0.067f, _p.ipd, 1e-6f);
            ipd.StepBy(_p, -1); ipd.StepBy(_p, -1);
            Assert.AreEqual(66.0, ipd.Get(_p), 1e-6);
        }

        [Test]
        public void Stepping_ClampsAtLimits()
        {
            foreach (var spec in LensParameters.All)
            {
                for (int i = 0; i < 5000; i++) spec.StepBy(_p, 1);
                Assert.AreEqual(spec.Max, spec.Get(_p), 1e-6, spec.Label + " max");
                for (int i = 0; i < 20000; i++) spec.StepBy(_p, -1);
                Assert.AreEqual(spec.Min, spec.Get(_p), 1e-6, spec.Label + " min");
            }
        }

        [Test]
        public void StepUpThenDown_ReturnsToStart_NoDrift()
        {
            foreach (var spec in LensParameters.All)
            {
                double start = spec.Get(_p);
                for (int i = 0; i < 15; i++) spec.StepBy(_p, 1);
                for (int i = 0; i < 15; i++) spec.StepBy(_p, -1);
                Assert.AreEqual(start, spec.Get(_p), 1e-4, spec.Label);
            }
        }

        [Test]
        public void K1_StepIsHundredth()
        {
            var k1 = LensParameters.Get(LensParameter.K1);
            k1.StepBy(_p, 1);
            Assert.AreEqual(0.451f, _p.k1, 1e-5f);
        }

        [Test]
        public void Clamp_HandlesNaNAndInfinity()
        {
            var spec = LensParameters.Get(LensParameter.FovLimit);
            Assert.AreEqual(spec.Min, spec.Clamp(double.NaN));
            Assert.AreEqual(spec.Min, spec.Clamp(double.PositiveInfinity));
            Assert.AreEqual(spec.Max, spec.Clamp(1000));
        }

        [Test]
        public void ClampAll_FixesOutOfRangeProfile()
        {
            _p.ipd = 0.5f; _p.k1 = 99f; _p.lensCenterYOffset = -1f;
            LensParameters.ClampAll(_p);
            Assert.AreEqual(0.08f, _p.ipd, 1e-6f);
            Assert.AreEqual(1.5f, _p.k1, 1e-6f);
            Assert.AreEqual(-0.02f, _p.lensCenterYOffset, 1e-6f);
        }

        [Test]
        public void Format_ShowsUnits()
        {
            Assert.AreEqual("66.5 mm", LensParameters.Get(LensParameter.Ipd).Format(_p));
            Assert.AreEqual("60 deg", LensParameters.Get(LensParameter.FovLimit).Format(_p));
        }
    }

    public class LensProfileStoreTests
    {
        private const string Dir = "/data/lens-profiles";
        private MemoryFileStore _fs;
        private LensProfileStore _store;
        private LensProfile _p;

        [SetUp]
        public void SetUp()
        {
            _fs = new MemoryFileStore();
            _store = new LensProfileStore(_fs, Dir);
            _p = LensProfile.CreateDefault();
        }

        [TearDown] public void TearDown() { Object.DestroyImmediate(_p); }

        [Test]
        public void SafeName_RemovesTraversalAndOddCharacters()
        {
            Assert.AreEqual("etc-passwd", LensProfileStore.SafeName("../../etc/passwd"));
            Assert.AreEqual("evil", LensProfileStore.SafeName("..\\..\\evil"));
            Assert.AreEqual("my-headset-v2", LensProfileStore.SafeName("  My Headset (v2) "));
            Assert.AreEqual("a_b", LensProfileStore.SafeName("A_B"));
            Assert.IsNull(LensProfileStore.SafeName(""));
            Assert.IsNull(LensProfileStore.SafeName(null));
            Assert.IsNull(LensProfileStore.SafeName("../.."));
            Assert.LessOrEqual(LensProfileStore.SafeName(new string('x', 200)).Length, LensProfileStore.MaxNameLength);
        }

        [Test]
        public void Save_WithTraversalName_StaysInsideFolder()
        {
            var saved = _store.Save("../../outside", _p);
            Assert.AreEqual("outside", saved);
            foreach (var key in _fs.Files.Keys) StringAssert.StartsWith(Dir + "/", key);
            Assert.IsTrue(_fs.Files.ContainsKey(Dir + "/outside.json"));
        }

        [Test]
        public void Save_UnusableName_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _store.Save("///", _p));
        }

        [Test]
        public void RoundTrip_KeepsValues()
        {
            _p.ipd = 0.063f; _p.k1 = 0.3f; _p.k2 = 0.2f; _p.screenToLensDistance = 0.045f;
            _p.maxHalfFovDegrees = 55f; _p.lensCenterYOffset = 0.002f;
            _store.Save("Test One", _p);
            var b = LensProfile.CreateDefault();
            try
            {
                Assert.IsTrue(_store.TryLoad("test-one", b));
                Assert.AreEqual(0.063f, b.ipd, 1e-6f);
                Assert.AreEqual(0.3f, b.k1, 1e-6f);
                Assert.AreEqual(0.2f, b.k2, 1e-6f);
                Assert.AreEqual(0.045f, b.screenToLensDistance, 1e-6f);
                Assert.AreEqual(55f, b.maxHalfFovDegrees, 1e-4f);
                Assert.AreEqual(0.002f, b.lensCenterYOffset, 1e-6f);
            }
            finally { Object.DestroyImmediate(b); }
        }

        [Test]
        public void TryLoad_MissingOrCorrupt_ReturnsFalseAndLeavesTargetUnchanged()
        {
            Assert.IsFalse(_store.TryLoad("nope", _p));
            _fs.Files[Dir + "/bad.json"] = "{ this is not json";
            _p.k1 = 0.123f;
            Assert.IsFalse(_store.TryLoad("bad", _p));
            Assert.AreEqual(0.123f, _p.k1, 1e-6f);
        }

        [Test]
        public void TryLoad_ClampsHandEditedValues()
        {
            _p.ipd = 0.5f;
            _fs.Files[Dir + "/wild.json"] = _p.ToJson();
            var b = LensProfile.CreateDefault();
            try
            {
                Assert.IsTrue(_store.TryLoad("wild", b));
                Assert.AreEqual(0.08f, b.ipd, 1e-6f);
            }
            finally { Object.DestroyImmediate(b); }
        }

        [Test]
        public void List_ReturnsSortedProfileNames_IgnoresOtherFiles()
        {
            _store.Save("zeta", _p);
            _store.Save("alpha", _p);
            _store.SetLastUsed("alpha");
            _fs.Files[Dir + "/Weird Name.json"] = "{}";
            var names = _store.List();
            CollectionAssert.AreEqual(new[] { "alpha", "zeta" }, names);
        }

        [Test]
        public void NextFreeName_SkipsTakenNames()
        {
            _store.Save("headset-1", _p);
            _store.Save("headset-2", _p);
            Assert.AreEqual("headset-3", _store.NextFreeName("headset"));
        }

        [Test]
        public void LastUsed_PersistsAcrossStoreInstances()
        {
            Assert.IsNull(_store.LastUsed);
            _store.Save("viewer-a", _p);
            _store.SetLastUsed("Viewer-A");
            var again = new LensProfileStore(_fs, Dir);
            Assert.AreEqual("viewer-a", again.LastUsed);
        }

        [Test]
        public void LastUsed_IgnoredWhenFileIsGone()
        {
            _fs.Files[Dir + "/" + LensProfileStore.LastUsedFile] = "ghost";
            Assert.IsNull(_store.LastUsed);
        }

        [Test]
        public void LoadStartup_FirstRun_WritesAndLoadsDefault()
        {
            var name = _store.LoadStartup(_p);
            Assert.AreEqual("generic-5-7in-viewer", name);
            Assert.IsTrue(_fs.Files.ContainsKey(Dir + "/generic-5-7in-viewer.json"));
            Assert.AreEqual(0.0665f, _p.ipd, 1e-6f);
        }

        [Test]
        public void LoadStartup_UsesLastUsedWhenPresent()
        {
            _p.k1 = 0.25f;
            _store.Save("mine", _p);
            _store.SetLastUsed("mine");
            var b = LensProfile.CreateDefault();
            try
            {
                Assert.AreEqual("mine", _store.LoadStartup(b));
                Assert.AreEqual(0.25f, b.k1, 1e-6f);
            }
            finally { Object.DestroyImmediate(b); }
        }

        [Test]
        public void LoadStartup_CorruptDefault_IsRepaired()
        {
            _fs.Files[Dir + "/generic-5-7in-viewer.json"] = "garbage";
            Assert.AreEqual("generic-5-7in-viewer", _store.LoadStartup(_p));
            var b = LensProfile.CreateDefault();
            try { Assert.IsTrue(_store.TryLoad("generic-5-7in-viewer", b)); }
            finally { Object.DestroyImmediate(b); }
        }
    }

    public class GestureRecognizerTests
    {
        private const float W = 3088f, H = 1440f;

        private static TouchFrame F(float t, int n, float x, float y) { return new TouchFrame { Time = t, Count = n, X = x, Y = y }; }

        [Test]
        public void TapLeftAndRight()
        {
            var g = new GestureRecognizer(W, H);
            g.Update(F(0f, 1, 300, 700));
            Assert.AreEqual(GestureKind.TapLeft, g.Update(F(0.1f, 0, 0, 0)));
            g.Update(F(1f, 1, 2500, 700));
            Assert.AreEqual(GestureKind.TapRight, g.Update(F(1.1f, 0, 0, 0)));
        }

        [Test]
        public void SwipeUp_EmitsStepPerDistance()
        {
            var g = new GestureRecognizer(W, H);
            float step = g.StepPixels;
            g.Update(F(0f, 1, 1500, 400));
            Assert.AreEqual(GestureKind.None, g.Update(F(0.05f, 1, 1500, 400 + step * 0.5f)));
            Assert.AreEqual(GestureKind.StepUp, g.Update(F(0.1f, 1, 1500, 400 + step * 1.01f)));
            Assert.AreEqual(GestureKind.None, g.Update(F(0.15f, 1, 1500, 400 + step * 1.5f)));
            Assert.AreEqual(GestureKind.StepUp, g.Update(F(0.2f, 1, 1500, 400 + step * 2.02f)));
            Assert.AreEqual(GestureKind.None, g.Update(F(0.3f, 0, 0, 0)), "swipe end is not a tap");
        }

        [Test]
        public void SwipeDown_StepsDown()
        {
            var g = new GestureRecognizer(W, H);
            g.Update(F(0f, 1, 1500, 900));
            Assert.AreEqual(GestureKind.StepDown, g.Update(F(0.1f, 1, 1500, 900 - g.StepPixels * 1.01f)));
        }

        [Test]
        public void LongPress_FiresOnceAndIsNotATap()
        {
            var g = new GestureRecognizer(W, H);
            g.Update(F(0f, 1, 800, 700));
            Assert.AreEqual(GestureKind.None, g.Update(F(0.5f, 1, 802, 701)));
            Assert.AreEqual(GestureKind.LongPress, g.Update(F(0.9f, 1, 802, 701)));
            Assert.AreEqual(GestureKind.None, g.Update(F(1.5f, 1, 802, 701)));
            Assert.AreEqual(GestureKind.None, g.Update(F(1.6f, 0, 0, 0)));
        }

        [Test]
        public void SlowTouchWithoutMovement_IsNeitherTapNorStep()
        {
            var g = new GestureRecognizer(W, H);
            g.Update(F(0f, 1, 800, 700));
            Assert.AreEqual(GestureKind.None, g.Update(F(0.5f, 0, 0, 0)));
        }

        [Test]
        public void TwoFingerTap_Detected()
        {
            var g = new GestureRecognizer(W, H);
            g.Update(F(0f, 1, 800, 700));
            g.Update(F(0.05f, 2, 800, 700));
            Assert.AreEqual(GestureKind.TwoFingerTap, g.Update(F(0.2f, 0, 0, 0)));
        }

        [Test]
        public void TwoFingers_DoNotStepOrLongPress()
        {
            var g = new GestureRecognizer(W, H);
            g.Update(F(0f, 2, 800, 700));
            Assert.AreEqual(GestureKind.None, g.Update(F(0.1f, 2, 800, 1400)));
            Assert.AreEqual(GestureKind.None, g.Update(F(1.5f, 2, 800, 1400)));
        }
    }
}
