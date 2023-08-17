using Grid.Bot.Interfaces;
using Grid.Bot.Utility;

namespace Grid.Bot.Hooks
{
    public sealed class ShutdownHook : IConsoleHook
    {
        public char[] HookKeys => new[] { '\u0003', '\u001b' };

        public void Callback(char key) => SignalUtility.InvokeInteruptSignal();
    }
}
