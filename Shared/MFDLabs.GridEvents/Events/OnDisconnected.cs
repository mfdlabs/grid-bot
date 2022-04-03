/* Copyright MFDLABS Corporation. All rights reserved. */

using System;
using System.Threading.Tasks;

#if DEBUG || DEBUG_LOGGING_IN_PROD
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnDisconnected
    {
        public static Task Invoke(Exception ex)
        {
#if DEBUG || DEBUG_LOGGING_IN_PROD
            if (!(ex is TaskCanceledException && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.DebugAllowTaskCanceledExceptions))
                SystemLogger.Singleton.Error(ex);
#endif
            return Task.CompletedTask;
        }
    }
}
