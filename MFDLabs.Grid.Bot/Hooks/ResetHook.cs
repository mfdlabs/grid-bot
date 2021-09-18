using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class ResetHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { 'r', 'R' };

        public void Callback(char key)
        {
            SignalUtility.Singleton.InvokeUserSignal2(true);
        }
    }
}
