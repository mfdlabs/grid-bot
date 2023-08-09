/*
    File name: GridServerArbiter.cs
    Written By: @networking-owk
    Description: A helper to arbiter grid server instances to avoid single instanced crash exploits

    Copyright MFDLABS 2001-2022. All rights reserved.
*/

namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ServiceModel.Channels;

using Logging;
using Instrumentation;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

/*
Documentation: 

Pooling:
    If an instance is marked as Poolable, that means the arbiter can use that instance for random compute,
    say we are trying to execute a BatchJobEx, but we don't know what instance we want to do so, we will call
    on the methods that don't have the name parameter, this will call on GetOrCreateAvailableInstance and attempt
    to grab any available instance that is poolable, including persistent instances.

Grid Server Instances:
    Persistent -> Persistent instances are created once and server their purpose until the user requests a closure explicitly,
                  these instances require a name, and optionally can be pooled.
    Regular    ->
                  Single Use -> These instances serve a single purpose and are then destroyed after that work is complete.
                  Leased     -> These instances serve any action until their lease expires. Calling RenewLease will renew it's
                                saved lease timespan it was given on instantiation.

Instrumentation:
    Instrumentation consists of performance monitors for the arbiter itself, the arbiter's port allocation and the arbiter's instances.
    These will track an array of data, such as success and failures counts.

Port Allocation:
    Port allocation is fully managed in memory, and will check if:
        1. A port was recently created.
        2. A service on the machine is holding the port.
        3. The port is within it's range.
    Ports are randomly generated between the InclusiveStartPort and ExclusiveEndPort constants.
    Ports will be given a maximum attempts of MaximumAttemptsToFindPort to find a port that is available.

SOAP Methods:
    This supports all SOAP Methods in RCCService.wsdl and ComputeCloudServiceV2.wsdl
*/


/// <summary>
/// In-memory grid server arbiter.
/// </summary>
public class GridServerArbiter : GridServerArbiterBase, IGridServerArbiter
{
    #region |Setup|

    private static Binding _defaultHttpBinding;
    private static ICounterRegistry _defaultCounterRegistry;
    private static IGridServerArbiter _instance;

    /// <summary>
    /// Singleton instance of the grid server arbiter.
    /// </summary>
    public static IGridServerArbiter Singleton
    {
        get
        {
            if (_defaultHttpBinding == null)
                throw new ApplicationException("The http binding was null, please call SetBinding()");
            if (_defaultCounterRegistry == null)
                throw new ApplicationException("The counter registry was null, please call SetCounterRegistry()");

            _instance ??= new GridServerArbiter(
                _defaultCounterRegistry,
                Logger.Singleton,
                _defaultHttpBinding
            );

            return _instance;
        }
    }

    /// <summary>
    /// Set the default http binding for the grid server arbiter. This is required before calling Singleton.
    /// </summary>
    /// <param name="binding"></param>
    /// <exception cref="ArgumentNullException"><paramref name="binding"/> is <see langword="null" />.</exception>
    public static void SetDefaultHttpBinding(Binding binding)
        => _defaultHttpBinding = binding ?? throw new ArgumentNullException(nameof(binding));

    /// <summary>
    /// Set the default counter registry for the grid server arbiter. This is required before calling Singleton.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="counterRegistry"/> is <see langword="null" />.</exception>
    public static void SetCounterRegistry(ICounterRegistry counterRegistry)
        => _defaultCounterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));

    #endregion |Setup|

    #region |Instrumentation|

    private class GridServerArbiterPerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Arbiter.InMemoryArbiter";

        internal IRawValueCounter TotalInvocations { get; }
        internal IRawValueCounter TotalInvocationsThatSucceeded { get; }
        internal IRawValueCounter TotalInvocationsThatFailed { get; }
        internal IRawValueCounter TotalArbiteredGridServerInstancesOpened { get; }
        internal IRawValueCounter TotalPersistentArbiteredGridServerInstancesOpened { get; }
        internal IRawValueCounter TotalInvocationsThatHitTheSingleInstancedArbiter { get; }

        internal GridServerArbiterPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            TotalInvocations = counterRegistry.GetRawValueCounter(Category, "TotalInvocations");
            TotalInvocationsThatSucceeded = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatSucceeded");
            TotalInvocationsThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatFailed");
            TotalArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(Category, "TotalArbiteredGridServerInstancesOpened");
            TotalPersistentArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(Category, "TotalPersistentArbiteredGridServerInstancesOpened");
            TotalInvocationsThatHitTheSingleInstancedArbiter = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatHitTheSingleInstancedArbiter");
        }
    }

    #endregion |Instrumentation|

    #region |Private Members|

    private readonly List<IGridServerInstance> _instances = new();

    private readonly Binding _httpBinding;
    private readonly GridServerArbiterPerformanceMonitor _perfmon;
    private readonly ICounterRegistry _counterRegistry;
    private readonly IPortAllocator _portAllocator;
    private readonly IWebServerDeployer _webServerDeployer;
    private readonly IGridServerDeployer _gridServerDeployer;
    private readonly ILogger _logger;

    #endregion |Private Members|

    #region |Constructors|

    /// <summary>
    /// Construct a new instance of <see cref="GridServerArbiter"/>.
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/> used for instrumentation.</param>
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    /// <param name="httpBinding">The <see cref="Binding"/> used for HTTP communication.</param>
    /// <param name="portAllocator">The <see cref="IPortAllocator"/> used for port allocation.</param>
    /// <param name="webServerDeployer">The <see cref="IWebServerDeployer"/> used for web server deployment.</param>
    /// <param name="gridServerDeployer">The <see cref="IGridServerDeployer"/> used for grid server deployment.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="counterRegistry"/> is <see langword="null" />.
    /// - <paramref name="logger"/> is <see langword="null" />.
    /// - <paramref name="httpBinding"/> is <see langword="null" />.
    /// </exception>
    public GridServerArbiter(
        ICounterRegistry counterRegistry,
        ILogger logger,
        Binding httpBinding,
        IPortAllocator portAllocator = null,
        IWebServerDeployer webServerDeployer = null,
        IGridServerDeployer gridServerDeployer = null
    )
    {
        _counterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpBinding = httpBinding ?? throw new ArgumentNullException(nameof(httpBinding));

        _portAllocator = portAllocator ?? new PortAllocator(counterRegistry, logger);

        var healthCheckClient = new WebServerHealthCheckClient(
            global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckBaseUrl,
            global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckExpectedResponseText
        );
        _webServerDeployer = webServerDeployer ?? new WebServerDeployer(
            logger,
            healthCheckClient,
            global::MFDLabs.Grid.Properties.Settings.Default.WebServerWorkspacePath
        );


        _gridServerDeployer = gridServerDeployer ?? new GridServerDeployer(
            global::MFDLabs.Grid.Properties.Settings.Default.GridServerExecutableName,
            GridServerFileHelper.GetGridServerPath(true),
            counterRegistry,
            logger,
            portAllocator,
            false
        );

        _perfmon = new(counterRegistry);

        DiscoverInstances();
    }

    #endregion |Constructors|

    #region |Instance Helpers|

    private static EndpointAddress EndpointAddressFromIPEndPoint(IPEndPoint endpoint)
        => new(new UriBuilder() { Host = endpoint.Address.ToString(), Port = endpoint.Port }.Uri);

    private static EndpointAddress EndpointAddressFromIpAndPort(string ipAddress, int port)
        => new(new UriBuilder() { Host = ipAddress, Port = port }.Uri);

    /// <inheritdoc cref="GridServerArbiterBase.DiscoverInstances"/>
    public override IReadOnlyCollection<IGridServerInstance> DiscoverInstances()
    {
        var processes = _gridServerDeployer.DiscoverInstances();
        var instances = new List<IGridServerInstance>();

        foreach (var process in processes)
        {
            var instance = new LeasedGridServerInstance(
                lease: LeasedGridServerInstance.DefaultLease,
                counterRegistry: _counterRegistry,
                logger: _logger,
                gridServerArbiter: this,
                gridServerDeployer: _gridServerDeployer,
                httpBinding: _httpBinding,
                remoteAddress: EndpointAddressFromIPEndPoint(process.EndPoint),
                name: Guid.NewGuid().ToString(),
                gridServerProcess: process
            );

            lock (_instances)
                _instances.Add(instance);
            
            instances.Add(instance);
        }

        return instances;
    }

    /// <inheritdoc cref="GridServerArbiterBase.KillAllInstances"/>
    public override int KillAllInstances()
    {
        lock (_instances)
        {
            var instanceCount = _instances.Count;
            _logger.Debug("Disposing of all grid server instances");
            foreach (var instance in _instances.ToArray())
            {
                _logger.Debug("Disposing of grid server instance: {0}", instance.ToString());
                _portAllocator.RemovePortFromCacheIfExists(instance.Endpoint.Address.Uri.Port);
                _instances.Remove(instance);

                instance.Dispose();
            }

            _logger.Debug("{0} grid server instances were disposed of", instanceCount);

            _webServerDeployer.StopWebServer();

            return instanceCount;
        }
    }

    /// <inheritdoc cref="GridServerArbiterBase.KillInstanceByName(string, string)"/>
    public override bool KillInstanceByName(string name, string ipAddress = "127.0.0.1")
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out _)) throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        var instance = GetInstance(name, ipAddress);
        if (instance == null) return false;

        _portAllocator.RemovePortFromCacheIfExists(instance.Endpoint.Address.Uri.Port);
        lock (_instances)
            _instances.Remove(instance);

        instance.Dispose();
        return true;
    }

    /// <inheritdoc cref="GridServerArbiterBase.RemoveInstance(IGridServerInstance)"/>
    public override void RemoveInstance(IGridServerInstance instance)
    {
        lock (_instances)
        {
            _logger.Debug("Removing grid server instance: {0}", instance.ToString());

            _portAllocator.RemovePortFromCacheIfExists(instance.Endpoint.Address.Uri.Port);
            _instances.Remove(instance);
        }
    }

    /// <inheritdoc cref="GridServerArbiterBase.CreateInstance(string, int, string, bool)"/>
    public override IGridServerInstance CreateInstance(
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool startUp = true
    )
    {
        if (!IPAddress.TryParse(ipAddress, out _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

        var currentAllocatedPort = _portAllocator.FindNextAvailablePort();

        _webServerDeployer.LaunchWebServer();

        var instance = new GridServerInstance(
            counterRegistry: _counterRegistry,
            logger: _logger,
            gridServerArbiter: this,
            gridServerDeployer: _gridServerDeployer,
            httpBinding: _httpBinding,
            remoteAddress: EndpointAddressFromIpAndPort(ipAddress, currentAllocatedPort),
            name: name ?? Guid.NewGuid().ToString(),
            maxAttemptsToCallSoap: maxAttemptsToCallSoap,
            startNow: startUp
        );

        _logger.Debug("Queueing up arbitered instance '{0}' on host '{1}'",
            instance.Name,
            instance.Endpoint.Address.Uri.ToString()
        );

        lock (_instances)
            _instances.Add(instance);

        return instance;
    }

    /// <inheritdoc cref="GridServerArbiterBase.CreatePersistentInstance(string, int, string, bool, bool)"/>
    public override IGridServerInstance CreatePersistentInstance(
        string name,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        bool startUp = true
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

        var currentAllocatedPort = _portAllocator.FindNextAvailablePort();

        _webServerDeployer.LaunchWebServer();

        var instance = new GridServerInstance(
            counterRegistry: _counterRegistry,
            logger: _logger,
            gridServerArbiter: this,
            gridServerDeployer: _gridServerDeployer,
            httpBinding: _httpBinding,
            remoteAddress: EndpointAddressFromIpAndPort(ipAddress, currentAllocatedPort),
            name: name ?? Guid.NewGuid().ToString(),
            maxAttemptsToCallSoap: maxAttemptsToCallSoap,
            startNow: startUp,
            isPersistent: true,
            isPoolable: isPoolable
        );

        _logger.Debug("Queueing up persistent arbitered instance '{0}' on host '{1}'",
            instance.Name,
            instance.Endpoint.Address.Uri.ToString()
        );

        lock (_instances)
            _instances.Add(instance);

        return instance;
    }

    /// <inheritdoc cref="GridServerArbiterBase.CreateLeasedInstance(string, TimeSpan?, int, string, bool)"/>
    public override ILeasedGridServerInstance CreateLeasedInstance(
        string name = null,
        TimeSpan? lease = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool startUp = true
    )
    {
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

        var currentAllocatedPort = _portAllocator.FindNextAvailablePort();

        _webServerDeployer.LaunchWebServer();

        var instance = new LeasedGridServerInstance(
            lease: lease ?? LeasedGridServerInstance.DefaultLease,
            counterRegistry: _counterRegistry,
            logger: _logger,
            gridServerArbiter: this,
            gridServerDeployer: _gridServerDeployer,
            httpBinding: _httpBinding,
            remoteAddress: EndpointAddressFromIpAndPort(ipAddress, currentAllocatedPort),
            name: name ?? Guid.NewGuid().ToString(),
            maxAttemptsToCallSoap: maxAttemptsToCallSoap,
            startNow: startUp
        );

        _logger.Debug("Queueing up leased arbitered instance '{0}' on host '{1}' with lease '{2}'",
            instance.Name,
            instance.Endpoint.Address.Uri.ToString(),
            instance.Lease
        );

        lock (_instances)
            _instances.Add(instance);

        return instance;
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetAllInstances"/>
    public override IReadOnlyCollection<IGridServerInstance> GetAllInstances()
    {
        lock (_instances)
            return _instances.ToArray();
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetAllAvailableInstances"/>
    public override IReadOnlyCollection<IGridServerInstance> GetAllAvailableInstances()
    {
        lock (_instances)
            return _instances.Where(x => x.IsAvailable).ToArray();
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetInstance(string, string)"/>
    public override IGridServerInstance GetInstance(string name, string ipAddress = "127.0.0.1")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        lock (_instances)
            return (
                from instance in _instances
                where instance.Name == name && instance.Endpoint.Address.Uri.Host == ipAddress
                select instance
            ).FirstOrDefault();
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetPersistentInstance(string, string)"/>
    public override IGridServerInstance GetPersistentInstance(string name, string ipAddress = "127.0.0.1")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        lock (_instances)
            return (
                from instance in _instances
                where instance.Name == name && instance.Endpoint.Address.Uri.Host == ipAddress && instance.Persistent
                select instance
            ).FirstOrDefault();
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetLeasedInstance(string, string)"/>
    public override ILeasedGridServerInstance GetLeasedInstance(string name, string ipAddress = "127.0.0.1")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!IPAddress.TryParse(ipAddress, out var _))
            throw new ArgumentException("Invalid IP address", nameof(ipAddress));

        lock (_instances)
            return (
                from instance in _instances
                let leasedInstance = instance as LeasedGridServerInstance
                where leasedInstance != null && leasedInstance.Name == name && leasedInstance.Endpoint.Address.Uri.Host == ipAddress
                select leasedInstance
            ).FirstOrDefault();
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetAvailableInstance"/>
    public override IGridServerInstance GetAvailableInstance()
    {
        lock (_instances)
            return (
                from instance in _instances
                where instance.IsPoolable && instance.IsAvailable
                select instance
            ).FirstOrDefault();
    }

    /// <inheritdoc cref="GridServerArbiterBase.GetAvailableLeasedInstance"/>
    public override ILeasedGridServerInstance GetAvailableLeasedInstance()
    {
        lock (_instances)
            return (
                from instance in _instances
                let leasedInstance = instance as LeasedGridServerInstance
                where leasedInstance != null && leasedInstance.IsPoolable && leasedInstance.IsAvailable
                select leasedInstance
            ).FirstOrDefault();
    }

    
    #endregion |Instance Helpers|

    #region |Invocation Helpers|

    /// <inheritdoc cref="GridServerArbiterBase.InvokeSoap{T}(string, string, int, string, bool, object[])"/>
    protected override T InvokeSoap<T>(
        string method,
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        params object[] args
    )
    {
        _perfmon.TotalInvocations.Increment();

        TryGetSoapMethod(args, false, method, out var methodToInvoke);

        _webServerDeployer.LaunchWebServer();

        var instance = GetOrCreateAnyInstance(name, maxAttemptsToCallSoap, ipAddress, isPoolable);

        _logger.Debug("Got the instance '{0}' to execute method '{1}'", instance, method);

        return ActuallyInvoke<T>(args, methodToInvoke, instance);
    }

    /// <inheritdoc cref="GridServerArbiterBase.InvokeSoapAsync{T}(string, string, int, string, bool, object[])"/>
    protected override async Task<T> InvokeSoapAsync<T>(
        string method,
        string name = null,
        int maxAttemptsToCallSoap = 5,
        string ipAddress = "127.0.0.1",
        bool isPoolable = false,
        params object[] args
    )
    {
        _perfmon.TotalInvocations.Increment();

        TryGetSoapMethod(args, true, method, out var methodToInvoke);

        _webServerDeployer.LaunchWebServer();

        var instance = GetOrCreateAnyInstance(name, maxAttemptsToCallSoap, ipAddress, isPoolable);

        _logger.Debug("Got the instance '{0}' to execute method '{1}'", instance, method);

        return await ActuallyInvokeAsync<T>(args, methodToInvoke, instance);
    }

    private T ActuallyInvoke<T>(object[] args, MethodInfo methodToInvoke, IGridServerInstance instance)
    {
        try
        {
            var result = (T)methodToInvoke.Invoke(instance, args);
            _perfmon.TotalInvocationsThatSucceeded.Increment();
            return result;
        }
        catch (TargetInvocationException ex)
        {
            _perfmon.TotalInvocationsThatFailed.Increment();
            if (ex.InnerException != null)
                throw ex.InnerException;

            throw;
        }
        finally
        {
            TryCleanupInstance(instance);
        }
    }

    private async Task<T> ActuallyInvokeAsync<T>(object[] args, MethodInfo methodToInvoke, IGridServerInstance instance)
    {
        try
        {
            var result = await ((Task<T>)methodToInvoke.Invoke(instance, args)).ConfigureAwait(false);
            _perfmon.TotalInvocationsThatSucceeded.Increment();
            return result;
        }
        catch (TargetInvocationException ex)
        {
            _perfmon.TotalInvocationsThatFailed.Increment();

            if (ex.InnerException != null)
                throw ex.InnerException;

            throw;
        }
        finally
        {
            TryCleanupInstance(instance);
        }
    }

    private void TryCleanupInstance(IGridServerInstance instance)
    {
        if (instance.Persistent) return;

        _portAllocator.RemovePortFromCacheIfExists(instance.Endpoint.Address.Uri.Port);

        lock (_instances)
            _instances.Remove(instance);

        instance.Dispose();
    }

    private static void TryGetSoapMethod(IEnumerable<object> args, bool isAsync, string lastMethod, out MethodInfo methodToInvoke)
    {
        methodToInvoke = typeof(GridServerInstance)
                .GetMethod(
                    lastMethod,
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    args.Select(x => x.GetType())
                       .ToArray(),
                    null
                );

        if (methodToInvoke == null)
            throw new ApplicationException($"Unknown grid server method '{lastMethod}'.");

        if (isAsync && !(methodToInvoke.ReflectedType == typeof(Task) || methodToInvoke.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            throw new ApplicationException($"Grid server method '{lastMethod}' is not async.");
    }

    #endregion |Invocation Helpers|
}
