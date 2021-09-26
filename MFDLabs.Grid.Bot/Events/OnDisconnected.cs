﻿using System;
using System.Threading.Tasks;

#if DEBUG
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnDisconnected
    {
        internal static Task Invoke(Exception ex)
        {
#if DEBUG
            if (!(ex is TaskCanceledException && !Settings.Singleton.DebugAllowTaskCanceledExceptions))
                SystemLogger.Singleton.Error(ex);
#endif
            return Task.CompletedTask;
        }
    }
}
