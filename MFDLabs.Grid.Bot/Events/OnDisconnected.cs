using MFDLabs.ErrorHandling;
using MFDLabs.Logging;
using System;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnDisconnected
    {
        internal static Task Invoke(Exception ex)
        {
#if DEBUG
            if (!(ex is TaskCanceledException && !Settings.Singleton.DebugAllowTaskCanceledExceptions))
                SystemLogger.Singleton.Error(new ExceptionDetail(ex).ToString());
#endif
            return Task.CompletedTask;
        }
    }
}
