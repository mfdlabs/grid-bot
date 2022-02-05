﻿using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class OpenGridServerHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'o', 'O' };

        public void Callback(char key)
        {
            if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                GridProcessHelper.OpenServerSafe();
            else 
                GridServerArbiter.Singleton.BatchQueueUpArbiteredInstances();
        }
    }
}
