/* Copyright MFDLABS Corporation. All rights reserved. */

namespace MFDLabs.Grid.Bot.Interfaces
{
    public interface IConsoleHook
    {
        char[] HookKeys { get; }
        void Callback(char key);
    }
}
