using System.Diagnostics;
using MFDLabs.Diagnostics;

namespace MFDLabs.Logging.Diagnostics
{
    public static class LoggingSystem
    {
        /// <summary>
        /// Shared log Sync so that we can block on each thread.
        /// </summary>
        internal static readonly object LogSync = new();
        public static Stopwatch GlobalLifetimeWatch { get; } = Stopwatch.StartNew();

        public static void EndLifetimeWatch()
        {
            SystemLogger.Singleton.LifecycleEvent("Ending event lifetime at {0} seconds into event life cycle...", GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("#"));
            GlobalLifetimeWatch?.Stop();
            GlobalLifetimeWatch?.Reset();
        }

        private static void StartLifetimeWatch()
        {
            SystemLogger.Singleton.LifecycleEvent("Starting event lifetime at '{0}'...", DateTimeGlobal.GetNowAsIso());
            GlobalLifetimeWatch.Start();
        }

        public static void RestartLifetimeWatch()
        {
            SystemLogger.Singleton.LifecycleEvent("Restarting event lifetime at {0} seconds into event life cycle...", GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("#"));
            EndLifetimeWatch();
            StartLifetimeWatch();
        }
    }
}
