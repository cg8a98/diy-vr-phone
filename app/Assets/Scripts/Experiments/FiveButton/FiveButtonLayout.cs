using UnityEngine;

namespace DiyVr.Experiments
{
    /// <summary>Pure maths for where the five buttons go, relative to the head when the test is switched on.</summary>
    public static class FiveButtonLayout
    {
        public const int ButtonCount = 5;
        public const float Distance = 1.5f;
        public const float StepDegrees = 15f;
        public const float Width = 0.12f;
        public const float Thickness = 0.01f;
        public const float LabelHeightAboveEye = 0.22f;

        /// <summary>Yaw angle of button i: -30, -15, 0, 15, 30 degrees (positive = right).</summary>
        public static float YawDegrees(int i)
        {
            return (i - (ButtonCount - 1) * 0.5f) * StepDegrees;
        }

        /// <summary>The head's forward direction with the pitch removed. Falls back to world forward when looking straight up/down.</summary>
        public static Vector3 FlatForward(Vector3 headForward)
        {
            Vector3 f = new Vector3(headForward.x, 0f, headForward.z);
            return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
        }

        /// <summary>World position of button i: on an arc of radius Distance around the head, at head height.</summary>
        public static Vector3 ButtonPosition(Vector3 headPos, Vector3 headForward, int i)
        {
            Vector3 dir = Quaternion.AngleAxis(YawDegrees(i), Vector3.up) * FlatForward(headForward);
            return headPos + dir * Distance;
        }
    }
}
