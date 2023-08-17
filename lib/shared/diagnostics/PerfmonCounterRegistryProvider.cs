using Instrumentation;

namespace Grid.Bot.PerformanceMonitors
{
    public static class PerfmonCounterRegistryProvider
    {
        public static ICounterRegistry Registry { get; } = new CounterRegistry();
    }
}
