using System;
using DiyVr.Stereo;

namespace DiyVr.Stereo.LensTool
{
    public enum LensParameter { K1 = 0, K2 = 1, FovLimit = 2, Ipd = 3, ScreenToLens = 4, VerticalOffset = 5 }

    /// <summary>One tunable value: label, unit, limits and step, all in the units shown on screen (mm, degrees).</summary>
    public sealed class LensParameterSpec
    {
        public readonly LensParameter Id;
        public readonly string Label;
        public readonly string Unit;
        public readonly double Min, Max, Step;
        public readonly int Decimals;
        private readonly Func<LensProfile, double> _get;
        private readonly Action<LensProfile, double> _set;

        public LensParameterSpec(LensParameter id, string label, string unit, double min, double max, double step, int decimals,
            Func<LensProfile, double> get, Action<LensProfile, double> set)
        {
            Id = id; Label = label; Unit = unit; Min = min; Max = max; Step = step; Decimals = decimals; _get = get; _set = set;
        }

        public double Get(LensProfile p) { return Math.Round(_get(p), 4); }

        public double Clamp(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return Min;
            return Math.Min(Max, Math.Max(Min, v));
        }

        public void Set(LensProfile p, double v) { _set(p, Clamp(Math.Round(v, 4))); }

        /// <summary>Moves one step up (+1) or down (-1), clamped to the limits. Returns the new value.</summary>
        public double StepBy(LensProfile p, int direction)
        {
            Set(p, Get(p) + Math.Sign(direction) * Step);
            return Get(p);
        }

        public string Format(LensProfile p)
        {
            return Get(p).ToString("F" + Decimals, System.Globalization.CultureInfo.InvariantCulture) + (Unit.Length > 0 ? " " + Unit : "");
        }
    }

    /// <summary>The list of tunable parameters, in the order the tuning screen shows them.</summary>
    public static class LensParameters
    {
        public static readonly LensParameterSpec[] All =
        {
            new LensParameterSpec(LensParameter.K1, "Distortion k1", "", -0.2, 1.5, 0.01, 2, p => p.k1, (p, v) => p.k1 = (float)v),
            new LensParameterSpec(LensParameter.K2, "Distortion k2", "", -0.5, 1.0, 0.01, 2, p => p.k2, (p, v) => p.k2 = (float)v),
            new LensParameterSpec(LensParameter.FovLimit, "Field of view limit", "deg", 20, 80, 1, 0, p => p.maxHalfFovDegrees, (p, v) => p.maxHalfFovDegrees = (float)v),
            new LensParameterSpec(LensParameter.Ipd, "Eye spacing (IPD)", "mm", 50, 80, 0.5, 1, p => p.ipd * 1000.0, (p, v) => p.ipd = (float)(v / 1000.0)),
            new LensParameterSpec(LensParameter.ScreenToLens, "Screen to lens", "mm", 20, 100, 0.5, 1, p => p.screenToLensDistance * 1000.0, (p, v) => p.screenToLensDistance = (float)(v / 1000.0)),
            new LensParameterSpec(LensParameter.VerticalOffset, "Vertical alignment", "mm", -20, 20, 0.5, 1, p => p.lensCenterYOffset * 1000.0, (p, v) => p.lensCenterYOffset = (float)(v / 1000.0)),
        };

        public static LensParameterSpec Get(LensParameter id) { return All[(int)id]; }

        /// <summary>Pulls every tunable value inside its limits (protects against hand-edited or corrupt files).</summary>
        public static void ClampAll(LensProfile p)
        {
            foreach (var s in All) s.Set(p, s.Get(p));
        }
    }
}
