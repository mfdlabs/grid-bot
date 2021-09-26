using System.Threading.Tasks;
using Discord;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnLogMessage
    {
        internal static Task Invoke(LogMessage message)
        {
            if (message.Exception != null)
            {
#if DEBUG
                if (!(message.Exception is TaskCanceledException && !Settings.Singleton.DebugAllowTaskCanceledExceptions))
                    SystemLogger.Singleton.Error("DiscordInternal-EXCEPTION-{0}: {1} {2}", message.Source, message.Message, message.Exception.ToDetailedString());
#endif
                return Task.CompletedTask;
            }

            if (Settings.Singleton.ShouldLogDiscordInternals)
            {
                switch (message.Severity)
                {
                    case LogSeverity.Warning:
                        SystemLogger.Singleton.Warning("DiscordInternal-WARNING-{0}: {1}", message.Source, message.Message);
                        break;
                    case LogSeverity.Debug:
                        SystemLogger.Singleton.Debug("DiscordInternal-DEBUG-{0}: {1}", message.Source, message.Message);
                        break;
                    case LogSeverity.Info:
                        SystemLogger.Singleton.Info("DiscordInternal-INFO-{0}: {1}", message.Source, message.Message);
                        break;
                    case LogSeverity.Verbose:
                        SystemLogger.Singleton.Log("DiscordInternal-VERBOSE-{0}: {1}", message.Source, message.Message);
                        break;
                    case LogSeverity.Error | LogSeverity.Critical:
                        SystemLogger.Singleton.Error("DiscordInternal-ERROR-{0}: {1}", message.Source, message.Message);
                        break;
                }
            }

            return Task.CompletedTask;
        }
    }
}
