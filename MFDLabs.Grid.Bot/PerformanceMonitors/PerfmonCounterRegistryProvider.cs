using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    internal static class PerfmonCounterRegistryProvider
    {
        public static ICounterRegistry Registry { get; } = new CounterRegistry();
    }
}
