using MFDLabs.Grid.Bot.Interfaces;
using System;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class ClearConsoleHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { 'c', 'C' };

        public void Callback(char key)
        {
            Console.Clear();
        }
    }
}
