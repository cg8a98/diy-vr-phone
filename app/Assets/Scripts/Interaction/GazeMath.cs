using UnityEngine;

namespace DiyVr.Interaction
{
    /// <summary>Pure helpers for the gaze pointer (no scene access, unit tested).</summary>
    public static class GazeMath
    {
        /// <summary>Default reticle size as seen by the eye, in degrees.</summary>
        public const float DefaultReticleDegrees = 1f;

        /// <summary>Distance (m) used for the reticle when the ray hits nothing.</summary>
        public const float DefaultMissDistance = 2f;

        /// <summary>World size (m) of a reticle that looks <paramref name="degrees"/> wide from <paramref name="distance"/> metres.</summary>
        public static float ReticleScale(float distance, float degrees)
        {
            if (distance <= 0f || degrees <= 0f) return 0f;
            return 2f * distance * Mathf.Tan(degrees * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>Distance to draw the reticle at: the hit distance, or <paramref name="missDistance"/> when nothing was hit.</summary>
        public static float ReticleDistance(bool hit, float hitDistance, float missDistance)
        {
            return hit && hitDistance > 0f ? hitDistance : missDistance;
        }
    }

    /// <summary>
    /// Remembers the current gaze target and reports enter/exit when it changes. Generic so it can be tested
    /// without Unity objects; the caller maps destroyed Unity objects to null before calling.
    /// </summary>
    public sealed class GazeTargetSwitch<T> where T : class
    {
        public T Current { get; private set; }

        /// <summary>Sets the new target. Returns true if it changed; <paramref name="exited"/> / <paramref name="entered"/> are the old and new (either may be null).</summary>
        public bool Update(T target, out T exited, out T entered)
        {
            exited = null;
            entered = null;
            if (ReferenceEquals(target, Current)) return false;
            exited = Current;
            entered = target;
            Current = target;
            return true;
        }
    }
}
