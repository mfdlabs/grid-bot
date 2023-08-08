using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class OpenGridServerHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'o', 'O' };

        public void Callback(char key)
        {
            GridServerArbiter.Singleton.BatchCreateLeasedInstances();
        }
    }
}
