using MFDLabs.Instrumentation;

namespace MFDLabs.Grid.Bot.PerformanceMonitors
{
    public static class PerfmonCounterRegistryProvider
    {
        public static ICounterRegistry Registry { get; } = new CounterRegistry();
    }
}
