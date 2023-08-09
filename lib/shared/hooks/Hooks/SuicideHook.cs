﻿using System;

using Logging;

using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Hooks
{
    public sealed class SuicideHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'f', 'F' };

        public void Callback(char key)
        {
            Logger.Singleton.Warning(
                "FORCE EXIT, ALL CACHED ITEMS WILL HAVE TO BE MANUALLY CLEARED. THIS IS POTENTIALLY DANGEROUS AS THE GLOBAL EVENT LIFETIME IS UNEXPECTEDLY CLOSED.");
            Environment.Exit(0);
        }
    }
}