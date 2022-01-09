using System;
using System.Threading.Tasks;

#if DEBUG
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnDisconnected
    {
        public static Task Invoke(Exception ex)
        {
#if DEBUG
            if (!(ex is TaskCanceledException && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.DebugAllowTaskCanceledExceptions))
                SystemLogger.Singleton.Error(ex);
#endif
            return Task.CompletedTask;
        }
    }
}
