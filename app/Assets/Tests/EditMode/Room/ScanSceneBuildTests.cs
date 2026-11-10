using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using DiyVr.Room;
using GLTFast;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace DiyVr.Tests
{
    /// <summary>
    /// Runs the real scan scene-building code (glTFast import, our material, texture limit, label) on a tiny
    /// generated .glb. v0.1.92 shipped a bug where scene-building code threw on the phone because only the maths
    /// was tested; these tests build the actual objects and fail on any error in the log.
    /// </summary>
    public class ScanSceneBuildTests
    {
        private LoadedScan _scan;
        private GameObject _head;

        [TearDown]
        public void TearDown()
        {
            if (_scan != null) _scan.Unload();
            _scan = null;
            if (_head != null) Object.DestroyImmediate(_head);
            _head = null;
            LogAssert.ignoreFailingMessages = false;
        }

        private IEnumerator Build(byte[] glb, int maxTexture)
        {
            // UninterruptedDeferAgent: load in one go (the default agent needs play mode).
            Task<LoadedScan> task = ScanSceneBuilder.BuildAsync(glb, "test-room", maxTexture, new UninterruptedDeferAgent());
            var watch = Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                if (watch.Elapsed.TotalSeconds > 60) Assert.Fail("loading the test scan timed out");
                yield return null;
            }
            Assert.IsFalse(task.IsFaulted, "BuildAsync must report errors, not throw: " + task.Exception);
            _scan = task.Result;
        }

        [UnityTest]
        public IEnumerator Build_TexturedGlb_MakesRenderersWithOurUnlitMaterial_AtRealScale()
        {
            yield return Build(SyntheticGlb.Build(true, 4), ScanLoadLogic.MaxTextureSize);

            Assert.IsNull(_scan.Error, "error");
            Assert.IsTrue(_scan.Ok);
            Assert.IsNotNull(_scan.Root, "root");
            Assert.AreEqual("test-room", _scan.Name);
            Assert.AreEqual(1, _scan.Triangles, "triangles");
            Assert.AreEqual(4, _scan.TexturePx, "texture size shown");
            Assert.AreEqual(4, _scan.SourceTexturePx, "texture size in the file");

            // Placement: at the origin, no rotation, scale 1 (1 unit = 1 m), and the 1 m triangle is 1 m big.
            Assert.AreEqual(Vector3.zero, _scan.Root.transform.position);
            Assert.AreEqual(Quaternion.identity, _scan.Root.transform.rotation);
            Assert.AreEqual(Vector3.one, _scan.Root.transform.lossyScale);

            var renderers = _scan.Root.GetComponentsInChildren<MeshRenderer>(true);
            Assert.AreEqual(1, renderers.Length, "renderers");
            foreach (var r in renderers)
            {
                Assert.IsNotNull(r.sharedMaterial, "material");
                Assert.IsNotNull(r.sharedMaterial.shader, "shader");
                Assert.AreEqual(ScanMaterialGenerator.ShaderName, r.sharedMaterial.shader.name, "our unlit shader");
                Assert.IsNotNull(r.sharedMaterial.mainTexture, "base colour texture");
                Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode);
                var filter = r.GetComponent<MeshFilter>();
                Assert.IsNotNull(filter, "mesh filter");
                Assert.IsNotNull(filter.sharedMesh, "mesh");
                Assert.AreEqual(1f, r.bounds.size.x, 1e-4f, "1 m wide");
                Assert.AreEqual(1f, r.bounds.size.y, 1e-4f, "1 m tall (Y is up)");
                Assert.AreEqual(0f, r.bounds.size.z, 1e-4f, "flat");
                Assert.AreEqual(0f, r.bounds.min.y, 1e-4f, "starts at the floor");
            }
            Assert.AreEqual("Scan: test-room, 1 tris, 4px", ScanLoadLogic.StatusText(_scan.Name, _scan.Triangles, _scan.TexturePx));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Build_UntexturedGlb_UsesThePlainColour()
        {
            yield return Build(SyntheticGlb.Build(false), ScanLoadLogic.MaxTextureSize);

            Assert.IsNull(_scan.Error, "error");
            var r = _scan.Root.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(r, "renderer");
            Assert.IsNotNull(r.sharedMaterial, "material");
            Assert.IsNull(r.sharedMaterial.mainTexture, "no texture in this file");
            Assert.Greater(r.sharedMaterial.color.r, 0.9f, "red");
            Assert.Less(r.sharedMaterial.color.g, 0.1f, "no green");
            Assert.AreEqual(0, _scan.TexturePx);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Build_FileWithoutMainScene_StillShowsTheFirstScene()
        {
            yield return Build(SyntheticGlb.Build(true, 4, false), ScanLoadLogic.MaxTextureSize);

            Assert.IsNull(_scan.Error, "error");
            Assert.AreEqual(1, _scan.Triangles);
            Assert.AreEqual(1, _scan.Root.GetComponentsInChildren<MeshRenderer>(true).Length);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Build_TextureAboveTheLimit_IsMadeSmaller()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("needs a graphics device (this test run has none); the size rule is covered by ScanLoadLogicTests");

            yield return Build(SyntheticGlb.Build(true, 16), 4);

            Assert.IsNull(_scan.Error, "error");
            Assert.AreEqual(16, _scan.SourceTexturePx, "size in the file");
            Assert.AreEqual(4, _scan.TexturePx, "size shown");
            var r = _scan.Root.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsNotNull(r.sharedMaterial.mainTexture, "texture");
            Assert.AreEqual(4, r.sharedMaterial.mainTexture.width);
            Assert.AreEqual(4, r.sharedMaterial.mainTexture.height);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Build_NotAGlb_ReportsTheError_WithoutThrowing()
        {
            yield return Build(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, ScanLoadLogic.MaxTextureSize);

            Assert.IsFalse(_scan.Ok);
            Assert.AreEqual("not a .glb file", _scan.Error);
            Assert.IsNull(_scan.Root);
            Assert.IsNull(GameObject.Find(ScanSceneBuilder.RootName), "nothing left in the scene");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Build_DamagedGlb_ReportsTheError_WithoutThrowing()
        {
            // Right header, but the description inside is rubbish. glTFast may log about it.
            LogAssert.ignoreFailingMessages = true;
            byte[] damaged = SyntheticGlb.Build(true, 4);
            int jsonLength = System.BitConverter.ToInt32(damaged, 12);
            for (int i = 0; i < jsonLength; i++) damaged[20 + i] = (byte)'x';

            yield return Build(damaged, ScanLoadLogic.MaxTextureSize);

            Assert.IsFalse(_scan.Ok);
            Assert.IsFalse(string.IsNullOrEmpty(_scan.Error), "an error text for the label");
            Assert.IsNull(_scan.Root);
            Assert.IsNull(GameObject.Find(ScanSceneBuilder.RootName), "nothing left in the scene");
        }

        [UnityTest]
        public IEnumerator Unload_RemovesTheScanFromTheScene()
        {
            yield return Build(SyntheticGlb.Build(true, 4), ScanLoadLogic.MaxTextureSize);
            Assert.IsNotNull(GameObject.Find(ScanSceneBuilder.RootName));

            _scan.Unload();

            Assert.IsNull(_scan.Root);
            Assert.IsNull(GameObject.Find(ScanSceneBuilder.RootName));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BuildLabel_MakesTextWithRendererAndMaterial_InFrontOfTheHead()
        {
            _head = new GameObject("Head");
            _head.transform.position = new Vector3(1f, 1.6f, 2f);

            TextMesh label = ScanLoadController.BuildLabel(_head.transform);

            Assert.IsNotNull(label, "label");
            Assert.AreEqual(_head.transform, label.transform.parent, "moves with the head");
            Assert.AreEqual(ScanLoadController.LabelOffset, label.transform.localPosition);
            Assert.IsNotNull(label.font, "font");
            var mr = label.GetComponent<MeshRenderer>();
            Assert.IsNotNull(mr, "text renderer");
            Assert.IsNotNull(mr.sharedMaterial, "text material");
            Assert.AreEqual(1, label.GetComponents<MeshRenderer>().Length, "exactly one renderer");
            label.text = ScanLoadLogic.StatusText("bedroom", 104212, 2048);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
