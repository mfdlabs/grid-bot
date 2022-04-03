/* Copyright MFDLABS Corporation. All rights reserved. */

using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class ResetHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'r', 'R' };

        public void Callback(char key) => SignalUtility.InvokeUserSignal2(true);
    }
}
