namespace DiyVr.Hands
{
    /// <summary>
    /// Crash guard for the GPU delegate. A native crash cannot be caught, so before trying GPU a "pending" flag is saved
    /// and cleared after the first good GPU result. If the flag is still set at the next start, the last try never
    /// finished (crash or kill) and GPU is blocked for good, so the app still starts and can be updated.
    /// </summary>
    public static class HandGpuGuard
    {
        public const string PendingKey = "hands.gpuPending";
        public const string OkKey = "hands.gpuOk";
        public const string BlockedKey = "hands.gpuBlocked";

        /// <summary>True when GPU may be tried. <paramref name="nowBlocked"/> is the blocked value to store.</summary>
        public static bool ShouldTryGpu(bool pending, bool blocked, out bool nowBlocked)
        {
            nowBlocked = blocked || pending;
            return !nowBlocked;
        }
    }
}
