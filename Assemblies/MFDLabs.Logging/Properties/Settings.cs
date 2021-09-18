using MFDLabs.Abstractions;
using MFDLabs.EventLog;

namespace MFDLabs.Logging
{
    internal sealed class Settings : SingletonBase<Settings>
    {
        internal string LoggingUtilDataName
        {
            get
            {
                return global::MFDLabs.Logging.Properties.Settings.Default.LoggingUtilDataName;
            }
        }
        internal bool PersistLocalLogs
        {
            get
            {
                return global::MFDLabs.Logging.Properties.Settings.Default.PersistLocalLogs;
            }
        }
        internal LogLevel MaxLogLevel
        {
            get
            {
                return global::MFDLabs.Logging.Properties.Settings.Default.MaxLogLevel;
            }
        }
    }
}
