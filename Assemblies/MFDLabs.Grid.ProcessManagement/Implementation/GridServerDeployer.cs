namespace MFDLabs.Grid;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Logging;
using Instrumentation;

/// <inheritdoc cref="IGridServerDeployer"/>
public class GridServerDeployer : IGridServerDeployer
{
    private class GridServerDeployerPerformanceMonitor
    {
        private const string CategoryName = "MFDLabs.Grid.GridServerDeployer";

        internal IRateOfCountsPerSecondCounter GridServerDeployerAttemptsPerSecond { get; set; }
        internal IRateOfCountsPerSecondCounter GridServerDeployerSuccessesPerSecond { get; set; }
        internal IRateOfCountsPerSecondCounter GridServerDeployerFailuresPerSecond { get; set; }

        public GridServerDeployerPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            GridServerDeployerAttemptsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(CategoryName, nameof(GridServerDeployerAttemptsPerSecond));
            GridServerDeployerSuccessesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(CategoryName, nameof(GridServerDeployerSuccessesPerSecond));
            GridServerDeployerFailuresPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(CategoryName, nameof(GridServerDeployerFailuresPerSecond));
        }
    }

    private static readonly IRandom _rng = RandomFactory.GetDefaultRandom();

    private readonly string _gridServerProcessName;
    private readonly string _executableName;
    private readonly string _executablePath;
    private readonly string _fullyQualifiedExecutable;

    private readonly ILogger _logger;
    private readonly IPortAllocator _portAllocator;
    private readonly GridServerDeployerPerformanceMonitor _perfmon;
    private readonly List<IGridServerProcess> _processes = new();

    /// <summary>
    /// Construct a new instance of <see cref="GridServerDeployer"/>
    /// </summary>
    /// <param name="exectableName">The name of the grid-server executable.</param>
    /// <param name="executablePath">The path to the grid-server executable.</param>
    /// <param name="counterRegistry">A counter registry for instrumentation.</param>
    /// <param name="logger">A logger for logging important messages.</param>
    /// <param name="portAllocator">An optional port allocator, will default to a new instance of <see cref="PortAllocator"/>.</param>
    /// <param name="discoverNow">Should we call on <see cref="DiscoverInstances"/> immediately?</param>
    /// <exception cref="ArgumentException">
    /// - <paramref name="exectableName"/> cannot be null or empty.
    /// - <paramref name="executablePath"/> cannot be null or empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="counterRegistry"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// </exception>
    public GridServerDeployer(
        string exectableName,
        string executablePath,
        ICounterRegistry counterRegistry,
        ILogger logger,
        IPortAllocator portAllocator,
        bool discoverNow = true
    )
    {
        if (string.IsNullOrEmpty(exectableName))
            throw new ArgumentException($"{nameof(exectableName)} cannot be null or empty.", nameof(exectableName));
        if (string.IsNullOrEmpty(executablePath))
            throw new ArgumentException($"{nameof(executablePath)} cannot be null or empty.", nameof(executablePath));
        if (counterRegistry == null) if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

        _executableName = exectableName;
        _gridServerProcessName = Path.GetFileNameWithoutExtension(exectableName);
        _executablePath = executablePath;
        _fullyQualifiedExecutable = Path.Combine(executablePath, exectableName);

        _perfmon = new(counterRegistry);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _portAllocator = portAllocator ?? new PortAllocator(counterRegistry, logger);

        if (discoverNow)
            DiscoverInstances();
    }

    /// <inheritdoc cref="IGridServerDeployer.Processes"/>
    public IEnumerable<IGridServerProcess> Processes => _processes;

    /// <inheritdoc cref="IGridServerDeployer.ExecutableName"/>
    public string ExecutableName => _executableName;

    /// <inheritdoc cref="IGridServerDeployer.ExecutablePath"/>
    public string ExecutablePath => _executablePath;

    /// <inheritdoc cref="IGridServerDeployer.KillAll"/>
    public void KillAll()
    {
        _perfmon.GridServerDeployerAttemptsPerSecond.Increment();
        _logger.Information("Kill all managed grid-server processes...");

        lock (_processes)
            _processes.ForEach(
                process =>
                {
                    _logger.Debug("Killing process '{0}'...", process.ToString());

                    try
                    {
                        process.Kill();
                        process.Dispose();

                        _processes.Remove(process);

                        _perfmon.GridServerDeployerSuccessesPerSecond.Increment();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                        _perfmon.GridServerDeployerFailuresPerSecond.Increment();
                    }
                }
            );
    }

    /// <inheritdoc cref="IGridServerDeployer.CreateProcess(int?, out IGridServerProcess, out Exception)"/>
    public bool CreateProcess(int? port, out IGridServerProcess gridServerProcess, out Exception exception)
    {
        gridServerProcess = null;
        exception = null;

        _perfmon.GridServerDeployerAttemptsPerSecond.Increment();
        _logger.Information("Starting new grid-server process...");

        try
        {
            gridServerProcess = GridServerProcess.Start(
                _fullyQualifiedExecutable,
                _executablePath,
                port ?? _portAllocator.FindNextAvailablePort()
            );

            gridServerProcess.SetLogger(_logger);

            lock (_processes)
                _processes.Add(gridServerProcess);

            _perfmon.GridServerDeployerSuccessesPerSecond.Increment();
            _logger.Debug("Started new grid-server process '{0}'.", gridServerProcess.ToString());

            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning("Failed to start new grid-server process: {0}", ex.Message);
            _perfmon.GridServerDeployerFailuresPerSecond.Increment();

            exception = ex;

            return false;
        }
    }

    /// <inheritdoc cref="IGridServerDeployer.GetProcess(int)"/>
    public IGridServerProcess GetProcess(int port)
    {
        _logger.Information("Try get grid-server process by port of '{0}'.", port);

        lock (_processes)
            return _processes
                .Where(process => process.EndPoint.Port == port)
                .FirstOrDefault();
    }

    /// <inheritdoc cref="IGridServerDeployer.GetProcessByPid(int)"/>
    public IGridServerProcess GetProcessByPid(int pid)
    {
        _logger.Information("Try get grid-server process by pid of '{0}'.", pid);

        lock (_processes)
            return _processes
                .Where(process => process.Process.Id == pid)
                .FirstOrDefault();
    }

    /// <inheritdoc cref="IGridServerDeployer.KillProcess(int, out Exception)"/>
    public bool KillProcess(int port, out Exception exception)
    {
        exception = null;

        _perfmon.GridServerDeployerAttemptsPerSecond.Increment();
        _logger.Information("Kill grid-server process by port of '{0}'.", port);

        lock (_processes)
        {
            var process = GetProcess(port);

            if (process == null)
            {
                _logger.Warning("No grid-server process found by port of '{0}'.", port);
                _perfmon.GridServerDeployerFailuresPerSecond.Increment();

                return false;
            }

            try
            {
                var processName = process.ToString();

                process.Kill();
                process.Dispose();

                _processes.Remove(process);

                _perfmon.GridServerDeployerSuccessesPerSecond.Increment();
                _logger.Debug("Killed grid-server process '{0}'.", processName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to kill grid-server process by port of '{0}': {1}", port, ex.Message);
                _perfmon.GridServerDeployerFailuresPerSecond.Increment();

                exception = ex;

                return false;
            }
        }
    }

    /// <inheritdoc cref="IGridServerDeployer.KillProcess(IGridServerProcess, out Exception)"/>
    public bool KillProcess(IGridServerProcess process, out Exception exception)
    {
        exception = null;

        _perfmon.GridServerDeployerAttemptsPerSecond.Increment();
        _logger.Information("Kill grid-server process '{0}'.", process.ToString());

        lock (_processes)
        {
            if (!_processes.Contains(process))
            {
                _logger.Warning("No grid-server process found by '{0}'.", process.ToString());
                _perfmon.GridServerDeployerFailuresPerSecond.Increment();

                return false;
            }

            try
            {
                var processName = process.ToString();

                process.Kill();
                process.Dispose();

                _processes.Remove(process);

                _perfmon.GridServerDeployerSuccessesPerSecond.Increment();
                _logger.Debug("Killed grid-server process '{0}'.", processName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to kill grid-server process '{0}': {1}", process.ToString(), ex.Message);
                _perfmon.GridServerDeployerFailuresPerSecond.Increment();

                exception = ex;

                return false;
            }
        }
    }

    /// <inheritdoc cref="IGridServerDeployer.KillProcessByPid(int, out Exception)"/>
    public bool KillProcessByPid(int pid, out Exception exception)
    {
        exception = null;

        _perfmon.GridServerDeployerAttemptsPerSecond.Increment();
        _logger.Information("Kill grid-server process by pid of '{0}'.", pid);

        lock (_processes)
        {
            var process = GetProcessByPid(pid);

            if (process == null)
            {
                _logger.Warning("No grid-server process found by pid of '{0}'.", pid);
                _perfmon.GridServerDeployerFailuresPerSecond.Increment();

                return false;
            }

            try
            {
                var processName = process.ToString();

                process.Kill();
                process.Dispose();

                _processes.Remove(process);

                _perfmon.GridServerDeployerSuccessesPerSecond.Increment();
                _logger.Debug("Killed grid-server process '{0}'.", processName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to kill grid-server process by pid of '{0}': {1}", pid, ex.Message);
                _perfmon.GridServerDeployerFailuresPerSecond.Increment();

                exception = ex;

                return false;
            }
        }
    }

    /// <inheritdoc cref="IGridServerDeployer.GetRandomProcess"/>
    public IGridServerProcess GetRandomProcess()
    {
        _logger.Information("Get random grid-server process.");
        
        lock (_processes)
        {
            if (_processes.Count == 0) return null;

            return _processes[_rng.Next(_processes.Count)];
        }
    }

    /// <inheritdoc cref="IGridServerDeployer.DiscoverInstances"/>
    public IEnumerable<IGridServerProcess> DiscoverInstances()
    {
        _logger.Information("Discovering existing processes...");

        var processes = GridServerProcess.DiscoverProcesses(_gridServerProcessName);

        lock (_processes)
            foreach (var process in processes)
            {
                if (!_processes.Contains(process))
                {
                    process.SetLogger(_logger);

                    _processes.Add(process);
                }
            }

        _logger.Debug("Discovered {0} existing processes.", processes.Count());

        return processes;
    }
}
