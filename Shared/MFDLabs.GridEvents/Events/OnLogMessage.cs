using System;
using System.Threading.Tasks;
using Discord;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnLogMessage
    {
        public static Task Invoke(LogMessage message)
        {
            if (message.Exception != null)
            {
#if !DEBUG_LOG_WEBSOCKET_CLOSED_EXCEPTIONS
                if (message.Exception?.InnerException is WebSocketClosedException) return Task.CompletedTask;
#endif

#if DEBUG || DEBUG_LOGGING_IN_PROD
                if (!(message.Exception is TaskCanceledException &&
                      !global::MFDLabs.Grid.Bot.Properties.Settings.Default.DebugAllowTaskCanceledExceptions))
                    Logger.Singleton.Error("DiscordInternal-EXCEPTION-{0}: {1} {2}",
                        message.Source,
                        message.Message,
                        message.Exception.ToString()
                    );
#endif
                return Task.CompletedTask;
            }

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLogDiscordInternals)
                return Task.CompletedTask;
            
            switch (message)
            {
                case {Severity: LogSeverity.Warning}:
                    Logger.Singleton.Warning("DiscordInternal-WARNING-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Debug}:
                    Logger.Singleton.Debug("DiscordInternal-DEBUG-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Info}:
                    Logger.Singleton.Info("DiscordInternal-INFO-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Verbose}:
                    Logger.Singleton.Verbose("DiscordInternal-VERBOSE-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Error | LogSeverity.Critical}:
                    Logger.Singleton.Error("DiscordInternal-ERROR-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Critical}:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }
    }
}
