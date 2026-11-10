using DiyVr.Experiments;
using DiyVr.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiyVr.Tests
{
    /// <summary>
    /// Builds the real five-button scene objects (not just the maths). v0.1.92 shipped with only one button because
    /// building the first number label threw, and no test ran the build code.
    /// </summary>
    public class FiveButtonBuildTests
    {
        private GameObject _host;
        private GameObject _head;

        [TearDown]
        public void TearDown()
        {
            var root = GameObject.Find("FiveButtonTest Buttons");
            if (root != null) Object.DestroyImmediate(root);
            if (_host != null) Object.DestroyImmediate(_host);
            if (_head != null) Object.DestroyImmediate(_head);
        }

        [Test]
        public void Build_MakesFiveGazeableButtons_NumbersAndLabel_WithoutErrors()
        {
            _head = new GameObject("Head");
            _host = new GameObject("FiveButtonTest");
            var test = _host.AddComponent<FiveButtonTest>();

            test.Build(_head.transform);

            var root = GameObject.Find("FiveButtonTest Buttons");
            Assert.IsNotNull(root, "button root missing");
            Assert.AreEqual(FiveButtonLayout.ButtonCount, root.GetComponentsInChildren<Gazeable>().Length, "gazeable buttons");
            Assert.AreEqual(FiveButtonLayout.ButtonCount, root.GetComponentsInChildren<BoxCollider>().Length, "button colliders");

            var texts = root.GetComponentsInChildren<TextMesh>();
            Assert.AreEqual(FiveButtonLayout.ButtonCount + 1, texts.Length, "five numbers plus the score label");
            foreach (var t in texts)
            {
                var mr = t.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr, "text renderer");
                Assert.IsNotNull(mr.sharedMaterial, "text material");
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Build_ButtonsAreSpreadOut_NotStacked()
        {
            _head = new GameObject("Head");
            _host = new GameObject("FiveButtonTest");
            _host.AddComponent<FiveButtonTest>().Build(_head.transform);

            var buttons = GameObject.Find("FiveButtonTest Buttons").GetComponentsInChildren<Gazeable>();
            for (int i = 1; i < buttons.Length; i++)
                Assert.Greater(Vector3.Distance(buttons[i].transform.position, buttons[i - 1].transform.position),
                    FiveButtonLayout.Width, "buttons " + i + " and " + (i + 1) + " overlap");
        }
    }
}
