using System;
using System.Threading.Tasks;

#if DEBUG
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Events
{
    internal static class OnDisconnected
    {
        internal static Task Invoke(Exception ex)
        {
#if DEBUG
            if (!(ex is TaskCanceledException && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.DebugAllowTaskCanceledExceptions))
                SystemLogger.Singleton.Error(ex);
#endif
            return Task.CompletedTask;
        }
    }
}
