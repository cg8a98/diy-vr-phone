using DiyVr.Hands;
using NUnit.Framework;

namespace DiyVr.Tests.Hands
{
    /// <summary>Ticket 2.2e: the maths that splits the camera age into "arrive" (camera pipeline) and "wait" (ours).</summary>
    public class CameraAgeProbeTests
    {
        private const double Boot = 5000.0;         // boot clock "now" at the start of a test
        private const double Slept = 1200.0;        // the phone slept 20 minutes since boot: mono = boot - Slept

        [Test]
        public void SplitsAgeIntoArriveAndWait()
        {
            var p = new CameraAgeProbe();
            // Picture exposed at 4999.880, Unity first sees it at 5000.000 (120 ms old), taken 14 ms later.
            p.OnCameraFrame(Boot - 0.120, Boot, 0.016);
            p.OnCameraFrame(Boot - 0.120, Boot + 0.008, 0.016); // same picture reported again: not a new arrival
            p.OnAcquire(Boot - 0.120, Boot + 0.014, Boot + 0.014 - Slept);

            Assert.AreEqual(120.0, p.ArriveMs, 1e-6);
            Assert.AreEqual(14.0, p.WaitMs, 1e-6);
            Assert.AreEqual(134.0, p.AgeMs, 1e-6);
            Assert.AreEqual(p.AgeMs, p.ArriveMs + p.WaitMs, 1e-6);
            Assert.AreEqual(16.0, p.ExposureMs, 1e-6);
            Assert.AreEqual(0.0, p.ImageMinusFrameMs, 1e-6);
        }

        [Test]
        public void ImageOlderThanArCoreFrame_ShowsNegativeDifference_AndNoWait()
        {
            var p = new CameraAgeProbe();
            p.OnCameraFrame(Boot - 0.100, Boot, -1);
            // The CPU image is the picture before ARCore's current frame (33 ms earlier).
            p.OnAcquire(Boot - 0.133, Boot + 0.002, Boot + 0.002 - Slept);

            Assert.AreEqual(-33.0, p.ImageMinusFrameMs, 1e-6);
            Assert.AreEqual(135.0, p.AgeMs, 1e-6);
            Assert.AreEqual(-1.0, p.WaitMs);      // we never saw that picture arrive, so wait is unknown
            Assert.AreEqual(-1.0, p.ExposureMs);  // phone gave no exposure time
        }

        [Test]
        public void AcquireBeforeTheCameraEvent_CountsAsTheArrival()
        {
            var p = new CameraAgeProbe();
            p.OnCameraFrame(Boot - 0.133, Boot - 0.033, -1);
            // Our Update ran first this frame: it is the first to see the new picture.
            p.OnAcquire(Boot - 0.100, Boot, Boot - Slept);
            p.OnCameraFrame(Boot - 0.100, Boot + 0.001, -1); // the event for the same picture must not count again

            Assert.AreEqual(100.0, p.ArriveMs, 1e-6); // both arrivals were 100 ms old
            Assert.AreEqual(0.0, p.WaitMs, 1e-6);
            Assert.AreEqual(33.0, p.ImageMinusFrameMs, 1e-6);
            Assert.AreEqual(30.3, p.CameraFps, 0.1);  // one 33 ms interval, not two
        }

        [Test]
        public void CameraFps_ComesFromTheStampsNotFromHowOftenWeAreCalled()
        {
            var p = new CameraAgeProbe();
            Assert.AreEqual(0.0, p.CameraFps);
            double stamp = Boot - 0.1;
            for (int i = 0; i < 10; i++)
            {
                // The app runs at 60 fps: every camera picture is reported twice.
                p.OnCameraFrame(stamp + i / 30.0, Boot + i / 30.0, -1);
                p.OnCameraFrame(stamp + i / 30.0, Boot + i / 30.0 + 1 / 60.0, -1);
            }
            Assert.AreEqual(30.0, p.CameraFps, 0.01);
            Assert.AreEqual(100.0, p.ArriveMs, 1e-6);
        }

        [Test]
        public void Clock_TellsBootFromMonotonic()
        {
            var p = new CameraAgeProbe();
            Assert.AreEqual("?", p.Clock);

            p.OnAcquire(Boot - 0.1, Boot, Boot - Slept);                 // stamp fits the boot clock only
            Assert.AreEqual("boot", p.Clock);

            p.Reset();
            p.OnAcquire(Boot - Slept - 0.1, Boot, Boot - Slept);         // stamp fits System.nanoTime only
            Assert.AreEqual("mono", p.Clock);
            Assert.AreEqual(-1.0, p.AgeMs);                              // our age maths cannot use it

            p.Reset();
            p.OnAcquire(Boot - 0.1, Boot, Boot);                         // phone never slept: clocks are equal
            Assert.AreEqual("boot=mono", p.Clock);

            p.Reset();
            p.OnAcquire(123.0, Boot, Boot - Slept);                      // fits neither
            Assert.AreEqual("?", p.Clock);
        }

        [Test]
        public void MissingStamps_AreIgnored()
        {
            var p = new CameraAgeProbe();
            p.OnCameraFrame(0.0, Boot, 0.010);
            p.OnAcquire(0.0, Boot, Boot);
            Assert.AreEqual(-1.0, p.ArriveMs);
            Assert.AreEqual(-1.0, p.AgeMs);
            Assert.IsFalse(p.HasImageMinusFrame);
            Assert.AreEqual(string.Empty, p.Lines());
            Assert.AreEqual(10.0, p.ExposureMs, 1e-6); // exposure is still kept
        }

        [Test]
        public void Lines_ShowEverything_AndQuestionMarksForUnknowns()
        {
            var p = new CameraAgeProbe();
            Assert.AreEqual(string.Empty, p.Lines());

            p.OnAcquire(Boot - 0.150, Boot, Boot - Slept); // no camera event yet
            Assert.AreEqual("CamAge 150 = arrive 150 + wait 0, exp ?\nCamClk boot, img-frm ?, ? fps", p.Lines());

            p.Reset();
            p.SetMaxModeFps(60);
            p.OnCameraFrame(Boot - 0.1533, Boot - 0.0333, 0.016);
            p.OnCameraFrame(Boot - 0.120, Boot, 0.016);
            p.OnAcquire(Boot - 0.120, Boot + 0.014, Boot + 0.014 - Slept);
            Assert.AreEqual("CamAge 134 = arrive 120 + wait 14, exp 16\nCamClk boot, img-frm 0, 30 fps (max 60)", p.Lines());
        }

        [Test]
        public void StatusLine_AppendsCameraAgeLines_AndSetOffClearsThem()
        {
            HandsStatus.SetOff("x");
            HandsStatus.Active = true;
            HandsStatus.HandCount = 1;
            HandsStatus.LandmarkFps = 24;
            HandsStatus.InferenceMs = 30;
            HandsStatus.LagTotalMs = 85;
            HandsStatus.LagAgeMs = 40;
            HandsStatus.LagConvertMs = 4;
            HandsStatus.CameraAgeLines = "CamAge 40 = arrive 30 + wait 10, exp 8";
            Assert.AreEqual("Hands 1, 24 fps, 30 ms\nLag 85 ms = cam 40 + cv 4 + inf 30 + post 11\nCamAge 40 = arrive 30 + wait 10, exp 8",
                HandsStatus.Line());
            HandsStatus.SetOff("done");
            Assert.AreEqual(string.Empty, HandsStatus.CameraAgeLines);
            Assert.AreEqual("Hands off: done", HandsStatus.Line());
        }
    }
}
