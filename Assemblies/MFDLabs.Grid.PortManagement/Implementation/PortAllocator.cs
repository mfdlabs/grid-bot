namespace MFDLabs.Grid;

using System;
using System.Linq;
using System.Diagnostics;
using System.Net.NetworkInformation;

using Microsoft.Extensions.Caching.Memory;

using Logging;
using Instrumentation;

/// <inheritdoc cref="IPortAllocator"/>
public class PortAllocator : IPortAllocator
{
    private class PortAllocationPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.PortManagement";

        internal IRateOfCountsPerSecondCounter PortAllocationAttemptsPerSecond { get; set; }
        internal IRateOfCountsPerSecondCounter PortAllocationSuccessesPerSecond { get; set; }
        internal IRateOfCountsPerSecondCounter PortAllocationFailuresPerSecond { get; set; }
        internal IAverageValueCounter PortAllocationSuccessAverageTimeTicks { get; set; }
        internal IAverageValueCounter PortAllocationFailureAverageTimeTicks { get; set; }

        public PortAllocationPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            PortAllocationAttemptsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "PortAllocationAttemptsPerSecond");
            PortAllocationSuccessesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "PortAllocationSuccessesPerSecond");
            PortAllocationFailuresPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "PortAllocationFailuresPerSecond");
            PortAllocationSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "PortAllocationSuccessAverageTimeTicks");
            PortAllocationFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "PortAllocationFailureAverageTimeTicks");
        }
    }

    private const int InclusiveStartPort = 45000;
    private const int ExclusiveEndPort = 47000;
    private const int MaximumAttemptsToFindPort = 1000;

    private static readonly IRandom _rng = RandomFactory.GetDefaultRandom();
    private static readonly TimeSpan _portReusedForbiddenDuration = TimeSpan.FromSeconds(30);

    private readonly ILogger _logger;
    private readonly PortAllocationPerformanceMonitor _perfmon;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly object _sync = new();

    /// <summary>
    /// Construct a new instance of <see cref="PortAllocator"/>
    /// </summary>
    /// <param name="counterRegistry">The counter registry.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="counterRegistry"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// </exception>
    public PortAllocator(ICounterRegistry counterRegistry, ILogger logger)
    {
        if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

        _perfmon = new(counterRegistry);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc cref="IPortAllocator.RemovePortFromCacheIfExists(int)"/>
    public void RemovePortFromCacheIfExists(int port) => _cache.Remove(port.ToString());

    /// <inheritdoc cref="IPortAllocator.FindNextAvailablePort"/>
    public int FindNextAvailablePort()
    {
        _perfmon.PortAllocationAttemptsPerSecond.Increment();
        var sw = Stopwatch.StartNew();

        lock (_sync)
        {
            for (int i = 0; i < MaximumAttemptsToFindPort; i++)
            {
                var port = _rng.Next(InclusiveStartPort, ExclusiveEndPort);

                if (IsPortInUse(port))
                {
                    _logger.Warning("Chosen random port, {0}, is already in use", port);
                    continue;
                }

                if (_cache.Get(port.ToString()) == null)
                {
                    _cache.Set(port.ToString(), string.Empty, DateTime.Now.Add(_portReusedForbiddenDuration));

                    sw.Stop();
                    _perfmon.PortAllocationSuccessesPerSecond.Increment();
                    _perfmon.PortAllocationSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                    _logger.Information(
                        "Port {0} is chosen for the next GridServerInstance. Number of attempts = {1}, time taken = {2} ms",
                        port,
                        i + 1,
                        sw.ElapsedMilliseconds
                    );

                    return port;
                }

                _logger.Warning("Chosen random port {0} has been used recently. Total number of recently used ports is {1}", port, _cache.Count);
            }
        }

        sw.Stop();
        _perfmon.PortAllocationFailuresPerSecond.Increment();
        _perfmon.PortAllocationFailureAverageTimeTicks.Sample(sw.ElapsedTicks);

        throw new TimeoutException(string.Format("Failed to find an open port. Time taken = {0} ms", sw.ElapsedMilliseconds));
    }

    private static bool IsPortInUse(int port)
        => (from listener in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners() select listener.Port).Contains(port);
}
