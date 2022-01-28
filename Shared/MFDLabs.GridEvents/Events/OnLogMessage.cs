using System;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnLogMessage
    {
        public static Task Invoke(LogMessage message)
        {
            if (message.Exception != null)
            {
                if (message.Exception?.InnerException is WebSocketClosedException) return Task.CompletedTask;

#if DEBUG || DEBUG_LOGGING_IN_PROD
                if (!(message.Exception is TaskCanceledException &&
                      !global::MFDLabs.Grid.Bot.Properties.Settings.Default.DebugAllowTaskCanceledExceptions))
                    SystemLogger.Singleton.Error("DiscordInternal-EXCEPTION-{0}: {1} {2}",
                        message.Source,
                        message.Message,
                        message.Exception.ToDetailedString());
#endif
                return Task.CompletedTask;
            }

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ShouldLogDiscordInternals)
                return Task.CompletedTask;
            
            switch (message)
            {
                case {Severity: LogSeverity.Warning}:
                    SystemLogger.Singleton.Warning("DiscordInternal-WARNING-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Debug}:
                    SystemLogger.Singleton.Debug("DiscordInternal-DEBUG-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Info}:
                    SystemLogger.Singleton.Info("DiscordInternal-INFO-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Verbose}:
                    SystemLogger.Singleton.Verbose("DiscordInternal-VERBOSE-{0}: {1}", message.Source, message.Message);
                    break;
                case {Severity: LogSeverity.Error | LogSeverity.Critical}:
                    SystemLogger.Singleton.Error("DiscordInternal-ERROR-{0}: {1}", message.Source, message.Message);
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
