using System;

namespace DiyVr.Stereo
{
    /// <summary>
    /// Radial lens distortion maths ported from the Google Cardboard SDK
    /// (github.com/googlevr/cardboard, Apache License 2.0; see docs/decisions.md).
    /// Original: sdk/distortion.cc, Distortion::DistortionFactor / Distort / DistortInverse.
    /// Re-implemented in C# with doubles; the inverse tolerance is tighter than the original's 1e-4.
    ///
    /// Model: a point at radius r (in units of screen-to-lens distance, measured from the lens centre)
    /// is shown at radius r' = r * (1 + k1 r^2 + k2 r^4).
    /// </summary>
    public static class CardboardDistortion
    {
        public const int MaxInverseIterations = 100;
        public const double InverseTolerance = 1e-12;

        /// <summary>1 + k1 r^2 + k2 r^4, given r squared.</summary>
        public static double DistortionFactor(double r2, double k1, double k2)
        {
            return 1.0 + k1 * r2 + k2 * r2 * r2;
        }

        public static double Distort(double r, double k1, double k2)
        {
            return r * DistortionFactor(r * r, k1, k2);
        }

        /// <summary>
        /// Finds r such that Distort(r) == target, using the secant method like the Cardboard SDK.
        /// Valid while Distort is increasing (true for the usual k1, k2 >= 0 and small negative values).
        /// </summary>
        public static double DistortInverse(double target, double k1, double k2)
        {
            if (target == 0.0) return 0.0;
            double r0 = target / 0.9;
            double r1 = target * 0.9;
            double dr0 = target - Distort(r0, k1, k2);
            for (int i = 0; i < MaxInverseIterations && Math.Abs(r1 - r0) > InverseTolerance; i++)
            {
                double dr1 = target - Distort(r1, k1, k2);
                double denom = dr1 - dr0;
                if (denom == 0.0) break;
                double r2 = r1 - dr1 * ((r1 - r0) / denom);
                r0 = r1;
                r1 = r2;
                dr0 = dr1;
            }
            return r1;
        }
    }
}
