using System.Diagnostics;
using MFDLabs.Diagnostics;

namespace MFDLabs.Logging.Diagnostics
{
    public static class LoggingSystem
    {
        /// <summary>
        /// Shared log Sync so that we can block on each thread.
        /// </summary>
        public static Stopwatch GlobalLifetimeWatch { get; } = Stopwatch.StartNew();

        public static void EndLifetimeWatch()
        {
            Logger.Singleton.LifecycleEvent("Ending event lifetime at {0} seconds into event life cycle...", GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("#"));
            GlobalLifetimeWatch?.Stop();
            GlobalLifetimeWatch?.Reset();
        }

        private static void StartLifetimeWatch()
        {
            Logger.Singleton.LifecycleEvent("Starting event lifetime at '{0}'...", DateTimeGlobal.GetNowAsIso());
            GlobalLifetimeWatch.Start();
        }

        public static void RestartLifetimeWatch()
        {
            Logger.Singleton.LifecycleEvent("Restarting event lifetime at {0} seconds into event life cycle...", GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("#"));
            EndLifetimeWatch();
            StartLifetimeWatch();
        }
    }
}
