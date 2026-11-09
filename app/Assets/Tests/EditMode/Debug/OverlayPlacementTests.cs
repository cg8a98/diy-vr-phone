using DiyVr.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace DiyVr.Tests.Diagnostics
{
    public class OverlayPlacementTests
    {
        [Test]
        public void LocalPosition_18Degrees_1p2Metres()
        {
            Vector3 p = OverlayPlacement.LocalPosition(1.2f, 18f);
            Assert.AreEqual(0f, p.x, 1e-5f);
            Assert.AreEqual(-0.3899f, p.y, 1e-3f);
            Assert.AreEqual(1.2f, p.z, 1e-5f);
        }

        [Test]
        public void LocalPosition_ZeroDegrees_IsStraightAhead()
        {
            Vector3 p = OverlayPlacement.LocalPosition(1.2f, 0f);
            Assert.AreEqual(0f, p.y, 1e-5f);
        }

        [Test]
        public void Rotation_FacesTheEye()
        {
            Vector3 p = OverlayPlacement.LocalPosition(2.0f, 22f);
            Vector3 forward = OverlayPlacement.LocalRotation(22f) * Vector3.forward;
            // The panel's forward points away from the eye, along the line from the eye to the panel.
            Assert.AreEqual(1f, Vector3.Dot(forward, p.normalized), 1e-4f);
        }

        [Test]
        public void Panel_IsNearTheGridDepth()
        {
            Assert.AreEqual(2.0f, OverlayPlacement.Distance, 1e-6f);
            Assert.LessOrEqual(Mathf.Abs(DiyVr.Stereo.PanelPlacement.GridDistance - OverlayPlacement.Distance), 0.5f);
        }

        [Test]
        public void Panel_TopEdgeIsBelowLensToolPanelBottomEdge()
        {
            float top = OverlayPlacement.TopEdgeDegreesDown(OverlayPlacement.Distance, OverlayPlacement.DegreesDown, OverlayPlacement.Height);
            Assert.AreEqual(15.64f, OverlayPlacement.LensPanelBottomDegrees, 0.05f);
            Assert.Greater(top, OverlayPlacement.LensPanelBottomDegrees + 1f, "needs a gap of at least 1 degree");
        }
    }
}
