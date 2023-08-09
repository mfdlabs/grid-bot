using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.Hooks
{
    internal sealed class ResetHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'r', 'R' };

        public void Callback(char key) => SignalUtility.InvokeUserSignal2(true);
    }
}
