using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using System;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class UtcNowHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { 't', 'T' };

        public void Callback(char key)
        {
            SystemLogger.Singleton.Log("The current time is '{0}'", DateTime.UtcNow.ToString());
        }
    }
}
