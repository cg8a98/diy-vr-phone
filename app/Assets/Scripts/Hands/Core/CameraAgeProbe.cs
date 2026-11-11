using System.Globalization;

namespace DiyVr.Hands
{
    /// <summary>
    /// Diagnostics for ticket 2.2e: splits the "cam" part of the hand lag into what we can and cannot change.
    ///
    ///   arrive = how old a camera frame already is the first time Unity can see it (sensor, phone camera pipeline,
    ///            ARCore). Not ours to cut from the hand code; only a different camera mode changes it.
    ///   wait   = how long that frame then sat until the hand worker was free to take it. This part is ours.
    ///   img-frm = CPU image stamp minus the stamp ARCore reports for the frame (ARCameraFrameEventArgs.timestampNs).
    ///            0 = same picture. Negative = the CPU image is an older picture than ARCore's current frame.
    ///   exp    = exposure time of the frame (the stamp is taken at the START of exposure, so half of this is not lag).
    ///   clock  = which Android clock the stamps agree with: "boot" (elapsedRealtimeNanos, what HandClock uses),
    ///            "mono" (System.nanoTime), "boot=mono" (phone has not slept since boot, cannot tell), "?" (neither).
    ///
    /// Pure maths, all times in seconds unless the name says Ms. Main thread only.
    /// </summary>
    public sealed class CameraAgeProbe
    {
        /// <summary>Two stamps closer than this are the same camera frame.</summary>
        public const double SameFrameSeconds = 1e-4;

        private readonly RollingAverage _arriveMs = new RollingAverage(30);
        private readonly RollingAverage _waitMs = new RollingAverage(20);
        private readonly RollingAverage _ageMs = new RollingAverage(20);
        private readonly RollingAverage _imgMinusFrameMs = new RollingAverage(20);
        private readonly RollingAverage _intervalMs = new RollingAverage(30);

        private double _seenStamp = -1;   // newest camera stamp seen so far, from either source
        private double _seenAt;           // HandClock time it was first seen
        private bool _seenAgeKnown;
        private double _eventStamp = -1;  // newest stamp reported by the camera frame event
        private double _exposureMs = -1;
        private int _maxModeFps;
        private bool _bootOk, _monoOk, _clocksDiffer, _clockSeen;

        public double ArriveMs { get { return _arriveMs.Count > 0 ? _arriveMs.Average : -1; } }
        public double WaitMs { get { return _waitMs.Count > 0 ? _waitMs.Average : -1; } }
        public double AgeMs { get { return _ageMs.Count > 0 ? _ageMs.Average : -1; } }
        public double ExposureMs { get { return _exposureMs; } }
        /// <summary>Camera frames per second measured from the stamps themselves; 0 until two frames were seen.</summary>
        public double CameraFps { get { return _intervalMs.Count > 0 && _intervalMs.Average > 0 ? 1000.0 / _intervalMs.Average : 0; } }
        public bool HasImageMinusFrame { get { return _imgMinusFrameMs.Count > 0; } }
        public double ImageMinusFrameMs { get { return _imgMinusFrameMs.Average; } }

        /// <summary>"boot", "mono", "boot=mono" or "?" (see class comment).</summary>
        public string Clock
        {
            get
            {
                if (!_clockSeen) return "?";
                if (_bootOk && _monoOk) return _clocksDiffer ? "boot" : "boot=mono";
                if (_bootOk) return "boot";
                return _monoOk ? "mono" : "?";
            }
        }

        public void Reset()
        {
            _arriveMs.Clear(); _waitMs.Clear(); _ageMs.Clear(); _imgMinusFrameMs.Clear(); _intervalMs.Clear();
            _seenStamp = -1; _eventStamp = -1; _exposureMs = -1; _seenAgeKnown = false;
            _bootOk = _monoOk = _clocksDiffer = _clockSeen = false;
        }

        /// <summary>Highest frame rate among the camera modes ARCore offers on this phone (0 = unknown).</summary>
        public void SetMaxModeFps(int fps) { _maxModeFps = fps; }

        /// <summary>The AR camera reported a frame (it repeats the same stamp until the camera delivers the next picture).</summary>
        /// <param name="frameStamp">ARCameraFrameEventArgs.timestampNs in seconds; 0 or less when missing.</param>
        /// <param name="now">HandClock.Now().</param>
        /// <param name="exposureSeconds">Exposure time, or less than 0 when the phone does not report it.</param>
        public void OnCameraFrame(double frameStamp, double now, double exposureSeconds)
        {
            if (exposureSeconds >= 0) _exposureMs = exposureSeconds * 1000.0;
            if (frameStamp <= 0) return;
            _eventStamp = frameStamp;
            Sight(frameStamp, now);
        }

        /// <summary>The hand service took a CPU image for a new inference.</summary>
        /// <param name="imageStamp">XRCpuImage.timestamp.</param>
        /// <param name="now">HandClock.Now() (boot clock).</param>
        /// <param name="nowMono">HandClock.NowMonotonic().</param>
        public void OnAcquire(double imageStamp, double now, double nowMono)
        {
            if (imageStamp <= 0) return;

            double bootAge = HandLatency.AgeSeconds(imageStamp, now);
            double monoAge = HandLatency.AgeSeconds(imageStamp, nowMono);
            _clockSeen = true;
            _bootOk = bootAge >= 0;
            _monoOk = monoAge >= 0;
            double gap = now - nowMono;
            _clocksDiffer = gap > 0.001 || gap < -0.001;

            if (_eventStamp > 0) _imgMinusFrameMs.Add((imageStamp - _eventStamp) * 1000.0);

            Sight(imageStamp, now); // in case our Update ran before the camera event for this picture
            if (bootAge < 0) return;
            _ageMs.Add(bootAge * 1000.0);
            // Wait is only known when the image is the newest frame we saw arrive.
            if (_seenAgeKnown && IsSame(imageStamp, _seenStamp))
            {
                double wait = now - _seenAt;
                if (wait >= 0 && wait <= HandLatency.MaxPlausibleAgeSeconds) _waitMs.Add(wait * 1000.0);
            }
        }

        private void Sight(double stamp, double now)
        {
            if (_seenStamp > 0 && stamp <= _seenStamp + SameFrameSeconds) return; // same picture again, or older
            if (_seenStamp > 0)
            {
                double interval = stamp - _seenStamp;
                if (interval > 0.001 && interval < 0.5) _intervalMs.Add(interval * 1000.0);
            }
            _seenStamp = stamp;
            _seenAt = now;
            double age = HandLatency.AgeSeconds(stamp, now);
            _seenAgeKnown = age >= 0;
            if (_seenAgeKnown) _arriveMs.Add(age * 1000.0);
        }

        private static bool IsSame(double a, double b)
        {
            double d = a - b;
            return d < SameFrameSeconds && d > -SameFrameSeconds;
        }

        /// <summary>
        /// Two overlay lines, e.g.
        /// "CamAge 134 = arrive 120 + wait 14, exp 16"
        /// "CamClk boot, img-frm 0, 30 fps (max 60)".
        /// Unknown values show as "?". Empty until the first CPU image was taken.
        /// </summary>
        public string Lines()
        {
            if (!_clockSeen) return string.Empty;
            return "CamAge " + Ms(AgeMs, false) + " = arrive " + Ms(ArriveMs, false) + " + wait " + Ms(WaitMs, false)
                + ", exp " + Ms(_exposureMs, false)
                + "\nCamClk " + Clock + ", img-frm " + (HasImageMinusFrame ? Ms(ImageMinusFrameMs, true) : "?")
                + ", " + (CameraFps > 0 ? CameraFps.ToString("0", CultureInfo.InvariantCulture) : "?") + " fps"
                + (_maxModeFps > 0 ? " (max " + _maxModeFps.ToString(CultureInfo.InvariantCulture) + ")" : string.Empty);
        }

        private static string Ms(double v, bool signed)
        {
            if (!signed && v < 0) return "?";
            string s = v.ToString("0", CultureInfo.InvariantCulture);
            return s == "-0" ? "0" : s;
        }
    }
}
