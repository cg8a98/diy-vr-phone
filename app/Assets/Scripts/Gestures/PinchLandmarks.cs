using UnityEngine;

namespace DiyVr.Gestures
{
    /// <summary>
    /// Turns MediaPipe landmarks into PinchDetector inputs. Space chosen: normalised image x,y with x multiplied by the
    /// image aspect (width/height) so distances are isotropic; z is ignored (relative depth is noisy). Hand scale is
    /// wrist (0) to middle-finger knuckle (9) in that same space, so the ratio does not depend on how far the hand is.
    /// </summary>
    public static class PinchLandmarks
    {
        public const int ThumbTip = 4;
        public const int IndexTip = 8;
        public const int Wrist = 0;
        public const int MiddleMcp = 9;

        public static bool TryGet(Vector3[] lm, float aspect, out Vector3 thumb, out Vector3 index, out float scale)
        {
            thumb = index = Vector3.zero;
            scale = 0f;
            if (lm == null || lm.Length < 21 || !(aspect > 0f)) return false;
            thumb = To2D(lm[ThumbTip], aspect);
            index = To2D(lm[IndexTip], aspect);
            scale = (To2D(lm[MiddleMcp], aspect) - To2D(lm[Wrist], aspect)).magnitude;
            return scale > 1e-5f;
        }

        private static Vector3 To2D(Vector3 p, float aspect)
        {
            return new Vector3(p.x * aspect, p.y, 0f);
        }
    }
}
