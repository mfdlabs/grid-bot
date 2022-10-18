/* Copyright MFDLABS Corporation. All rights reserved. */

using System;
using System.Globalization;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class UtcNowHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 't', 'T' };

        public void Callback(char key) 
            => Logger.Singleton.Log("The current time is '{0}'", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
    }
}
