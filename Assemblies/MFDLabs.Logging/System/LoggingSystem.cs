using System.Diagnostics;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;

namespace MFDLabs.Logging.Diagnostics
{
    public sealed class LoggingSystem : SingletonBase<LoggingSystem>
    {
        private readonly Stopwatch _globalWatch;

        public Stopwatch GlobalLifetimeWatch => _globalWatch;

        public LoggingSystem()
        {
            _globalWatch = Stopwatch.StartNew();
        }

        public void EndLifetimeWatch()
        {
            SystemLogger.Singleton.LifecycleEvent("Ending event lifetime at {0} seconds into event life cycle...", _globalWatch.Elapsed.TotalSeconds.ToString("#"));
            _globalWatch?.Stop();
            _globalWatch?.Reset();
        }

        public void StartLifetimeWatch()
        {
            SystemLogger.Singleton.LifecycleEvent("Starting event lifetime at '{0}'...", DateTimeGlobal.Singleton.GetNowAsISO());
            _globalWatch.Start();
        }

        public void RestartLifetimeWatch()
        {
            SystemLogger.Singleton.LifecycleEvent("Restarting event lifetime at {0} seconds into event life cycle...", _globalWatch.Elapsed.TotalSeconds.ToString("#"));
            EndLifetimeWatch();
            StartLifetimeWatch();
        }
    }
}
