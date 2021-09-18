using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class PartialShutdownHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { 'e', 'E' };

        public void Callback(char key)
        {
            SignalUtility.Singleton.InvokeUserSignal1();
        }
    }
}
