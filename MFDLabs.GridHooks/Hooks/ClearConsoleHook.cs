using System;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class ClearConsoleHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'c', 'C' };

        public void Callback(char key) => Console.Clear();
    }
}
