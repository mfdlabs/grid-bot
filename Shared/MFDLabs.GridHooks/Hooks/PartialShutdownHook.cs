/* Copyright MFDLABS Corporation. All rights reserved. */

using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class PartialShutdownHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'e', 'E' };

        public void Callback(char key) => SignalUtility.InvokeUserSignal1();
    }
}
