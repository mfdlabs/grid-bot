using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class CommandRegistryMetricsReportHook : IConsoleHook
    {
        public char[] HookKeys => new[] { 'm', 'M' };

        public void Callback(char key) => CommandRegistry.LogMetricsReport();
    }
}
