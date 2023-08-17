using System.ServiceModel;

using Logging;

using Grid.Bot.PerformanceMonitors;

namespace Grid.Bot.Utility
{
    public static class ScriptExecutionArbiter
    {
        public static IGridServerArbiter Singleton = new GridServerArbiter(
            PerfmonCounterRegistryProvider.Registry,
            Logger.Singleton,
            new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                SendTimeout = global::Grid.Bot.Properties.Settings.Default.ScriptExecutionArbiterMaxTimeout
            }
        );
    }
}
