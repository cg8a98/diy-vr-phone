using System;
using UnityEngine;

namespace DiyVr.Stereo
{
    /// <summary>
    /// Everything that describes one headset + phone combination. Lengths are metres.
    /// Can be saved as an asset or as JSON (ticket 1.6 will save per-headset JSON files).
    /// </summary>
    [CreateAssetMenu(menuName = "DIY VR/Lens Profile", fileName = "LensProfile")]
    public sealed class LensProfile : ScriptableObject
    {
        [Header("Phone screen (landscape, metres)")]
        public float screenWidth = 0.1564f;   // Galaxy S23 Ultra: 6.8 in, 3088x1440 -> about 156.4 x 72.9 mm
        public float screenHeight = 0.0729f;

        [Header("Headset geometry (metres)")]
        [Tooltip("Distance between the eyes (virtual camera separation). Default 66.5 mm.")]
        public float ipd = 0.0665f;
        [Tooltip("Distance between the two lens centres on the phone screen (metres). Cardboard v2 default 63.9 mm. Separate from ipd, which only sets the virtual camera separation.")]
        public float lensSpacing = 0.0639f;
        [Tooltip("Distance from screen to lens.")]
        public float screenToLensDistance = 0.042f;
        [Tooltip("Distance from lens to the eye (eye relief). Stored for the lens tool; the Cardboard model does not use it for the field of view.")]
        public float lensToEyeDistance = 0.018f;
        [Tooltip("How far the lens centre sits above the screen centre.")]
        public float lensCenterYOffset = 0f;

        [Header("Lens")]
        [Tooltip("Radial distortion r' = r (1 + k1 r^2 + k2 r^4). Cardboard v1 default 0.441, 0.156.")]
        public float k1 = 0.441f;
        public float k2 = 0.156f;
        [Tooltip("Largest allowed half field of view per side, degrees.")]
        [Range(20f, 80f)] public float maxHalfFovDegrees = 60f;

        [Header("Rendering")]
        [Range(0.3f, 1.5f)] public float renderScale = 1f;
        [Min(0.01f)] public float nearClip = 0.05f;
        [Min(1f)] public float farClip = 100f;

        /// <summary>Plain-data copy used by the maths, which never touches Unity objects.</summary>
        public StereoParams ToParams()
        {
            return new StereoParams
            {
                ScreenWidth = screenWidth,
                ScreenHeight = screenHeight,
                Ipd = ipd,
                LensSpacing = lensSpacing,
                ScreenToLens = screenToLensDistance,
                LensCenterYOffset = lensCenterYOffset,
                K1 = k1,
                K2 = k2,
                MaxHalfFovRadians = maxHalfFovDegrees * Mathf.Deg2Rad
            };
        }

        public string ToJson() { return JsonUtility.ToJson(this, true); }

        public void FromJson(string json) { JsonUtility.FromJsonOverwrite(json, this); }

        public static LensProfile CreateDefault() { return CreateInstance<LensProfile>(); }
    }

    /// <summary>Pure-data inputs for the stereo maths (doubles, no Unity types).</summary>
    [Serializable]
    public struct StereoParams
    {
        public double ScreenWidth, ScreenHeight, Ipd, LensSpacing, ScreenToLens, LensCenterYOffset, K1, K2, MaxHalfFovRadians;
    }
}
