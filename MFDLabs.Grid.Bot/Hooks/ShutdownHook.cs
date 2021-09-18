using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class ShutdownHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { '\u0003', '\u001b' };

        public void Callback(char key)
        {
            SignalUtility.Singleton.InvokeInteruptSignal();
        }
    }
}
