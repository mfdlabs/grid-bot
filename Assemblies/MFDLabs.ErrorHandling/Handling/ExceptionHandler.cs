using System;
using System.Diagnostics;

namespace MFDLabs.ErrorHandling
{
    public class ExceptionHandler
    {
        private static bool _shouldTryToUseEventLog = true;

        public static void LogException(Exception ex, EventLogEntryType eventLogEntryType, string eventSource = null)
        {
            LogException(new ExceptionDetail(ex).ToString(), eventLogEntryType, eventSource);
            LogRaw(ex);
        }

        private static void LogRaw(Exception ex)
        {
            LogRaw(new ExceptionDetail(ex).ToString());
        }

        public static void LogException(Exception ex)
        {
            LogException(ex, EventLogEntryType.Error);
        }

        public static void LogException(string message, EventLogEntryType eventLogEntryType, string eventSource = null)
        {
            if (_shouldTryToUseEventLog)
            {
                try
                {
                    if (eventSource == null)
                        eventSource = Guid.NewGuid().ToString();
                    EventLog log = new EventLog("MFDLabs");
                    EventLog.CreateEventSource(eventSource, "MFDLabs");
                    log.Source = eventSource;
                    log.WriteEntry(message, eventLogEntryType);

                }
                catch (Exception e)
                {
                    if (global::MFDLabs.ErrorHandling.Properties.Settings.Default.ShouldExplainEventLogExceptions)
                        LogRaw(e);
                    _shouldTryToUseEventLog = false;
                }
            }

            LogRaw(message);
        }

        private static void LogRaw(string message)
        {
            Console.WriteLine(message);
        }

        public static void LogException(string message)
        {
            LogException(message, EventLogEntryType.Error);
        }
    }
}
