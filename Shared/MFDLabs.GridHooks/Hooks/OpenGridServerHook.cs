using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class OpenGridServerHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'o', 'O' };

        public void Callback(char key)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                GridProcessHelper.OpenGridServerSafe();
            else 
                GridServerArbiter.Singleton.BatchQueueUpArbiteredInstances();
        }
    }
}
