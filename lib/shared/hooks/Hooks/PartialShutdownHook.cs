using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.Hooks
{
    internal sealed class PartialShutdownHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'e', 'E' };

        public void Callback(char key) => SignalUtility.InvokeUserSignal1();
    }
}
