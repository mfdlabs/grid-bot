/*
File name: GridServerArbiter.cs
Written By: @networking-owk
Description: A helper to arbiter grid server instances to avoid single instanced crash exploits

Copyright MFDLABS 2001-2022. All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Caching.Memory;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Concurrency;

#if DEBUG || DEBUG_LOGGING_IN_PROD
using MFDLabs.ErrorHandling.Extensions;
#endif

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

Associated Jira Tickets:
    GRIDBOT-7
    GRIDBOT-10
    GRIDBOT-12
*/

namespace MFDLabs.Grid
{
    // In here check for SingleInstancedGridServer
    // if true, piggyback off SoapHelper :)

    // so what if we have 2 instances with the same name but on different ports?
    // should we queue them up regardless, or only queue them if it's not persistent
    // seems about right :)
    public sealed class GridServerArbiter
    {
        #region |Setup|

        private static Binding _defaultHttpBinding;
        private static ICounterRegistry _counterRegistry;

        private static GridServerArbiter _instance;

        public static GridServerArbiter Singleton
        {
            get
            {
                if (_defaultHttpBinding == null)
                    throw new ApplicationException("The http binding was null, please call SetBinding()");

                if (_instance == null) _instance = new();

                return _instance;
            }
        }

        public static void SetDefaultHttpBinding(Binding binding) => _defaultHttpBinding = binding ?? throw new ArgumentNullException(nameof(binding));
        public static void SetCounterRegistry(ICounterRegistry counterRegistry) => _counterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));

        public void SetupPool()
            => BatchQueueUpLeasedArbiteredInstancesUnsafe(
                null,
                DefaultPoolSize
#if DEBUG
                ,
                5,
                "localhost",
                false
#endif
            );

        #endregion |Setup|

        #region |Constants|

        private const string BaseClassHelloWorldMethodName = "HelloWorld";
        private const string BaseClassHelloWorldAsyncMethodName = "HelloWorldAsync";
        private const string BaseClassGetVersionMethodName = "GetVersion";
        private const string BaseClassGetVersionAsyncMethodName = "GetVersionAsync";
        private const string BaseClassGetStatusMethodName = "GetStatus";
        private const string BaseClassGetStatusAsyncMethodName = "GetStatusAsync";
        private const string BaseClassOpenJobMethodName = "OpenJob";
        private const string BaseClassOpenJobAsyncMethodName = "OpenJobAsync";
        private const string BaseClassOpenJobExMethodName = "OpenJobEx";
        private const string BaseClassOpenJobExAsyncMethodName = "OpenJobExAsync";
        private const string BaseClassRenewLeaseMethodName = "RenewLease";
        private const string BaseClassRenewLeaseAsyncMethodName = "RenewLeaseAsync";
        private const string BaseClassExecuteMethodName = "Execute";
        private const string BaseClassExecuteAsyncMethodName = "ExecuteAsync";
        private const string BaseClassExecuteExMethodName = "ExecuteEx";
        private const string BaseClassExecuteExAsyncMethodName = "ExecuteExAsync";
        private const string BaseClassCloseJobMethodName = "CloseJob";
        private const string BaseClassCloseJobAsyncMethodName = "CloseJobAsync";
        private const string BaseClassBatchJobMethodName = "BatchJob";
        private const string BaseClassBatchJobAsyncMethodName = "BatchJobAsync";
        private const string BaseClassBatchJobExMethodName = "BatchJobEx";
        private const string BaseClassBatchJobExAsyncMethodName = "BatchJobExAsync";
        private const string BaseClassGetExpirationMethodName = "GetExpiration";
        private const string BaseClassGetExpirationAsyncMethodName = "GetExpirationAsync";
        private const string BaseClassGetAllJobsMethodName = "GetAllJobs";
        private const string BaseClassGetAllJobsAsyncMethodName = "GetAllJobsAsync";
        private const string BaseClassGetAllJobsExMethodName = "GetAllJobsEx";
        private const string BaseClassGetAllJobsExAsyncMethodName = "GetAllJobsExAsync";
        private const string BaseClassCloseExpiredJobsMethodName = "CloseExpiredJobs";
        private const string BaseClassCloseExpiredJobsAsyncMethodName = "CloseExpiredJobsAsync";
        private const string BaseClassCloseAllJobsMethodName = "CloseAllJobs";
        private const string BaseClassCloseAllJobsAsyncMethodName = "CloseAllJobsAsync";
        private const string BaseClassDiagMethodName = "Diag";
        private const string BaseClassDiagAsyncMethodName = "DiagAsync";
        private const string BaseClassDiagExMethodName = "DiagEx";
        private const string BaseClassDiagExAsyncMethodName = "DiagExAsync";

#if DEBUG
        private const int DefaultPoolSize = 5;
#else
        private const int DefaultPoolSize = 25;
#endif

        #endregion |Constants|

        #region |Instrumentation|

        private class PortAllocationPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.Arbiter.PortAllocation.PerfmonV2";

            internal IRateOfCountsPerSecondCounter PortAllocationAttemptsPerSecond { get; set; }
            internal IRateOfCountsPerSecondCounter PortAllocationSuccessesPerSecond { get; set; }
            internal IRateOfCountsPerSecondCounter PortAllocationFailuresPerSecond { get; set; }
            internal IAverageValueCounter PortAllocationSuccessAverageTimeTicks { get; set; }
            internal IAverageValueCounter PortAllocationFailureAverageTimeTicks { get; set; }

            public PortAllocationPerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                PortAllocationAttemptsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "PortAllocationAttemptsPerSecond", instance);
                PortAllocationSuccessesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "PortAllocationSuccessesPerSecond", instance);
                PortAllocationFailuresPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "PortAllocationFailuresPerSecond", instance);
                PortAllocationSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "PortAllocationSuccessAverageTimeTicks", instance);
                PortAllocationFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "PortAllocationFailureAverageTimeTicks", instance);
            }

        }

        private class GridServerInstancePerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.Arbiter.Instance.PerfmonV2";

            internal IRawValueCounter TotalInvocations { get; }
            internal IRawValueCounter TotalInvocationsThatSucceeded { get; }
            internal IRawValueCounter TotalInvocationsThatFailed { get; }

            internal GridServerInstancePerformanceMonitor(ICounterRegistry counterRegistry, GridServerInstance inst)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{inst.Name} -> http://{inst.Endpoint.Address.Uri.Host}:{inst.Port}";

                TotalInvocations = counterRegistry.GetRawValueCounter(Category, "TotalInvocations", instance);
                TotalInvocationsThatSucceeded = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatSucceeded", instance);
                TotalInvocationsThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatFailed", instance);
            }
        }

        private class GridServerArbiterPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.Arbiter.PerfmonV2";

            internal IRawValueCounter TotalInvocations { get; }
            internal IRawValueCounter TotalInvocationsThatSucceeded { get; }
            internal IRawValueCounter TotalInvocationsThatFailed { get; }
            internal IRawValueCounter TotalArbiteredGridServerInstancesOpened { get; }
            internal IRawValueCounter TotalPersistentArbiteredGridServerInstancesOpened { get; }
            internal IRawValueCounter TotalInvocationsThatHitTheSingleInstancedArbiter { get; }

            internal GridServerArbiterPerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalInvocations = counterRegistry.GetRawValueCounter(Category, "TotalInvocations", instance);
                TotalInvocationsThatSucceeded = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatSucceeded", instance);
                TotalInvocationsThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatFailed", instance);
                TotalArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(Category, "TotalArbiteredGridServerInstancesOpened", instance);
                TotalPersistentArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(Category, "TotalPersistentArbiteredGridServerInstancesOpened", instance);
                TotalInvocationsThatHitTheSingleInstancedArbiter = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatHitTheSingleInstancedArbiter", instance);
            }
        }

        #endregion |Instrumentation|

        #region |Port Allocation|

        private static class PortAllocation
        {
            #region |Constants|

            private const int InclusiveStartPort = 45000;
            private const int ExclusiveEndPort = 47000;
            private const int MaximumAttemptsToFindPort = 1000;

            #endregion |Constants|

            #region |Performance|

            internal static PortAllocationPerformanceMonitor _perfmon;

            #endregion |Performance|

            #region |Thread Safety|

            private static readonly object _sync = new();

            #endregion |Thread Safety|

            private static readonly IRandom _rng = RandomFactory.GetDefaultRandom();
            private static readonly TimeSpan _portReusedForbiddenDuration = TimeSpan.FromSeconds(30);
            private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

            public static void RemovePortFromCacheIfExists(int port) => _cache.Remove(port.ToString());

            private static bool IsPortInUse(int port)
                => (from listener in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners() select listener.Port).Contains(port);

            public static int FindNextAvailablePort()
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
                            SystemLogger.Singleton.Warning("Chosen random port, {0}, is already in use", port);
                            continue;
                        }

                        if (_cache.Get(port.ToString()) == null)
                        {
                            _cache.Set(port.ToString(), string.Empty, DateTime.Now.Add(_portReusedForbiddenDuration));
                            sw.Stop();
                            _perfmon.PortAllocationSuccessesPerSecond.Increment();
                            _perfmon.PortAllocationSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);

                            SystemLogger.Singleton.Info(
                                "Port {0} is chosen for the next GridServerInstance. Number of attempts = {1}, time taken = {2} ms",
                                port,
                                i + 1,
                                sw.ElapsedMilliseconds
                            );
                            return port;
                        }
                        SystemLogger.Singleton.Warning("Chosen random port {0} has been used recently. Total number of recently used ports is {1}", port, _cache.Count);
                    }
                }
                sw.Stop();
                _perfmon.PortAllocationFailuresPerSecond.Increment();
                _perfmon.PortAllocationFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                throw new TimeoutException(string.Format("Failed to find an open port. Time taken = {0} ms", sw.ElapsedMilliseconds));
            }
        }

        #endregion |Port Allocation|

        #region |Private Members|

        private readonly List<GridServerInstance> _instances = new();
        private GridServerArbiterPerformanceMonitor _perfmon;

        private void CheckAndSetPerfmon()
        {
            if (!(_perfmon == null && PortAllocation._perfmon == null)) return;

            if (_counterRegistry == null) throw new ApplicationException("The counter registry was not set.");

            _perfmon = new(_counterRegistry);
            PortAllocation._perfmon = new(_counterRegistry);
        }

        #endregion |Private Members|

        #region |Instance Helpers|

        public IReadOnlyCollection<GridServerInstance> GetAllInstances()
        {
            lock (_instances)
                return _instances.ToArray();
        }

        public int KillAllOpenInstancesUnsafe()
        {
            var instanceCount = _instances.Count;
            SystemLogger.Singleton.LifecycleEvent("Disposing of all grid server instances");
            foreach (var instance in _instances.ToArray())
            {
                SystemLogger.Singleton.LifecycleEvent("Disposing of grid server instance: {0}", instance.ToString());
                PortAllocation.RemovePortFromCacheIfExists(instance.Port);
                _instances.Remove(instance);
                instance.Dispose();
            }
            return instanceCount;
        }

        public int KillAllOpenInstances()
        {
            lock (_instances)
            {
                var instanceCount = _instances.Count;
                SystemLogger.Singleton.LifecycleEvent("Disposing of all grid server instances");
                foreach (var instance in _instances.ToArray())
                {
                    SystemLogger.Singleton.LifecycleEvent("Disposing of grid server instance: {0}", instance.ToString());
                    PortAllocation.RemovePortFromCacheIfExists(instance.Port);
                    _instances.Remove(instance);

                    // What is this hack here? Why does it not call inherited class's Dipose() method?
                    if (instance is LeasedGridServerInstance lI) lI.Dispose();
                    else instance.Dispose();
                }

                return instanceCount;
            }
        }

        public void RemoveInstanceFromQueue(GridServerInstance inst) => _instances.Remove(inst);

        public bool KillInstanceByNameUnsafe(string name, string hostName = "localhost")
        {
            var instance = GetInstance(name, hostName);
            if (instance == null) return false;

            PortAllocation.RemovePortFromCacheIfExists(instance.Port);
            _instances.Remove(instance);
            instance.Dispose();
            return true;
        }

        public bool KillInstanceByName(string name, string hostName = "localhost")
        {
            var instance = GetInstance(name, hostName);
            if (instance == null) return false;

            PortAllocation.RemovePortFromCacheIfExists(instance.Port);
            lock (_instances)
                _instances.Remove(instance);
            instance.Dispose();
            return true;
        }

        public List<GridServerInstance> BatchQueueUpArbiteredInstances(
            int count = 1,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true
        )
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            var instances = new List<GridServerInstance>();
            for (var i = 0; i < count; i++)
                instances.Add(QueueUpArbiteredInstance(null,
                    maxAttemptsToHitGridServer,
                    hostName,
                    startUp));
            return instances;
        }

        public List<LeasedGridServerInstance> BatchQueueUpLeasedArbiteredInstances(
            TimeSpan? lease = null,
            int count = 1,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true
        )
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            var instances = new List<LeasedGridServerInstance>();
            for (var i = 0; i < count; i++)
                instances.Add(QueueUpLeasedArbiteredInstance(null, lease, maxAttemptsToHitGridServer, hostName, startUp));
            return instances;
        }

        public List<GridServerInstance> BatchQueueUpArbiteredInstancesUnsafe(
            int count = 1,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true
        )
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            var instances = new List<GridServerInstance>();
            for (var i = 0; i < count; i++)
                instances.Add(QueueUpArbiteredInstanceUnsafe(null,
                    maxAttemptsToHitGridServer,
                    hostName,
                    startUp));
            return instances;
        }

        public List<LeasedGridServerInstance> BatchQueueUpLeasedArbiteredInstancesUnsafe(
            TimeSpan? lease = null,
            int count = 1,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true
        )
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            var instances = new List<LeasedGridServerInstance>();
            for (var i = 0; i < count; i++)
                instances.Add(QueueUpLeasedArbiteredInstanceUnsafe(null, lease, maxAttemptsToHitGridServer, hostName, startUp));
            return instances;
        }

        //warning: THIS HAS ZERO THREAD SAFETY !!!
        //it also pools start up, so we may not get the arbiter back for a while!!!!!!
        public GridServerInstance QueueUpArbiteredInstanceUnsafe(
            string name = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true,
            bool openNowInNewThread = true
        )
        {
            CheckAndSetPerfmon();

            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = PortAllocation.FindNextAvailablePort();

            GridProcessHelper.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                _counterRegistry,
                hostName,
                currentAllocatedPort,
                name ?? Guid.NewGuid().ToString(),
                startUp,
                maxAttemptsToHitGridServer,
                false,
                true,
                openNowInNewThread
            );
            SystemLogger.Singleton.Debug("Queueing up arbitered instance '{0}' on host '{1}'",
                instance.Name,
                instance.Endpoint.Address.Uri.ToString());
            _instances.Add(instance);
            return instance;
        }

        public LeasedGridServerInstance QueueUpLeasedArbiteredInstanceUnsafe(
            string name = null,
            TimeSpan? lease = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true,
            bool openNowInNewThread = true
        )
        {
            CheckAndSetPerfmon();

            TimeSpan newLease;

            if (lease == null) newLease = LeasedGridServerInstance.DefaultLease;
            else newLease = lease.Value;

            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = PortAllocation.FindNextAvailablePort();

            GridProcessHelper.OpenWebServerIfNotOpen();
            var instance = new LeasedGridServerInstance(
                _counterRegistry,
                newLease,
                hostName,
                currentAllocatedPort,
                name ?? Guid.NewGuid().ToString(),
                startUp,
                maxAttemptsToHitGridServer,
                true,
                openNowInNewThread
            );
            SystemLogger.Singleton.Debug("Queueing up leased arbitered instance '{0}' on host '{1}' with lease '{2}'",
                instance.Name,
                instance.Endpoint.Address.Uri.ToString(),
                newLease
            );
            _instances.Add(instance);
            return instance;
        }

        public GridServerInstance QueueUpPersistentArbiteredInstanceUnsafe(
            string name,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool isPoolable = false,
            bool startUp = true,
            bool openNowInNewThread = true
        )
        {
            CheckAndSetPerfmon();

            _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = PortAllocation.FindNextAvailablePort();

            GridProcessHelper.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                _counterRegistry,
                hostName,
                currentAllocatedPort,
                name,
                startUp,
                maxAttemptsToHitGridServer,
                true,
                isPoolable,
                openNowInNewThread
            );
            SystemLogger.Singleton.Debug("Queueing up persistent arbitered instance '{0}' on host '{1}'",
                instance.Name,
                instance.Endpoint.Address.Uri.ToString());
            _instances.Add(instance);
            return instance;
        }

        public GridServerInstance QueueUpArbiteredInstance(
            string name = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true,
            bool openNowInNewThread = false
        )
        {
            CheckAndSetPerfmon();

            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = PortAllocation.FindNextAvailablePort();

            GridProcessHelper.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                _counterRegistry,
                hostName,
                currentAllocatedPort,
                name ?? Guid.NewGuid().ToString(),
                startUp,
                maxAttemptsToHitGridServer,
                false,
                true,
                openNowInNewThread
            );
            SystemLogger.Singleton.Debug("Queueing up arbitered instance '{0}' on host '{1}'",
                instance.Name,
                instance.Endpoint.Address.Uri.ToString());
            lock (_instances)
                _instances.Add(instance);
            return instance;
        }

        public LeasedGridServerInstance QueueUpLeasedArbiteredInstance(
            string name = null,
            TimeSpan? lease = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true,
            bool openNowInNewThread = true
        )
        {
            CheckAndSetPerfmon();

            TimeSpan newLease;

            if (lease == null) newLease = LeasedGridServerInstance.DefaultLease;
            else newLease = lease.Value;

            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = PortAllocation.FindNextAvailablePort();

            GridProcessHelper.OpenWebServerIfNotOpen();
            var instance = new LeasedGridServerInstance(
                _counterRegistry,
                newLease,
                hostName,
                currentAllocatedPort,
                name ?? Guid.NewGuid().ToString(),
                startUp,
                maxAttemptsToHitGridServer,
                true,
                openNowInNewThread
            );
            SystemLogger.Singleton.Debug("Queueing up leased arbitered instance '{0}' on host '{1}' with lease '{2}'",
                instance.Name,
                instance.Endpoint.Address.Uri.ToString(),
                newLease
            );
            lock (_instances)
                _instances.Add(instance);
            return instance;
        }

        public GridServerInstance QueueUpPersistentArbiteredInstance(
            string name,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool isPoolable = false,
            bool startUp = true,
            bool openNowInNewThread = false
        )
        {
            CheckAndSetPerfmon();

            _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = PortAllocation.FindNextAvailablePort();

            GridProcessHelper.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                _counterRegistry,
                hostName,
                currentAllocatedPort,
                name,
                startUp,
                maxAttemptsToHitGridServer,
                true,
                isPoolable,
                openNowInNewThread
            );
            SystemLogger.Singleton.Debug("Queueing up persistent arbitered instance '{0}' on host '{1}'",
                instance.Name,
                instance.Endpoint.Address.Uri.ToString());
            lock (_instances)
                _instances.Add(instance);
            return instance;
        }

        public GridServerInstance GetInstance(string name, string hostName = "localhost")
        {
            lock (_instances)
                return (from instance in _instances
                        where instance.Name == name && instance.Endpoint.Address.Uri.Host == hostName
                        select instance).FirstOrDefault();
        }

        public GridServerInstance GetOrCreateInstance(string name, int maxAttemptsToHitGridServer = 5, string hostName = "hostname")
        {
            var instance = GetInstance(name, hostName);
            return instance ?? QueueUpArbiteredInstance(name, maxAttemptsToHitGridServer, hostName);
        }

        public GridServerInstance GetPersistentInstance(string name, string hostName = "localhost")
        {
            lock (_instances)
                return (from instance in _instances
                        where instance.Name == name && instance.Endpoint.Address.Uri.Host == hostName && instance.Persistent
                        select instance).FirstOrDefault();
        }

        public GridServerInstance GetOrCreatePersistentInstance(string name,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "hostname",
            bool isPoolable = false,
            bool openNowInNewThread = false)
        {
            var instance = GetPersistentInstance(name, hostName);
            return instance ?? QueueUpPersistentArbiteredInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable, openNowInNewThread);
        }

        public GridServerInstance GetAvailableInstance()
        {
            lock (_instances)
                return (from instance in _instances where instance.IsPoolable && instance.IsAvailable select instance).FirstOrDefault();
        }

        public LeasedGridServerInstance GetAvailableLeasedInstance()
        {
            lock (_instances)
                return (LeasedGridServerInstance)(from instance in _instances 
                        where instance.IsPoolable && instance.IsAvailable && 
                        instance.GetType() == typeof(LeasedGridServerInstance) select instance).FirstOrDefault();
        }

        public GridServerInstance GetOrCreateAvailableInstance(
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool openNowInNewThread = false
        )
        {
            var instance = GetAvailableInstance();
            return instance ?? QueueUpArbiteredInstance(null, maxAttemptsToHitGridServer, hostName, openNowInNewThread);

        }
        public LeasedGridServerInstance GetOrCreateAvailableLeasedInstance(
            TimeSpan? lease = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool openNowInNewThread = false
        )
        {
            var instance = GetAvailableLeasedInstance();
            return instance ?? QueueUpLeasedArbiteredInstance(null, lease, maxAttemptsToHitGridServer, hostName, openNowInNewThread);
        }

        private GridServerInstance GetOrCreateGridServerInstance(
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable
        )
        {
            var instance = !name.IsNullOrEmpty() ?
                GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable) :
                GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);
            return instance;
        }

        #endregion |Instance Helpers|

        #region |Invocation Helpers|

        private void InvokeMethod(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args
        )
            => InvokeMethod<object>(method, name, maxAttemptsToHitGridServer, hostName, isPoolable, args);
        private T InvokeMethod<T>(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args
        )
        {
            CheckAndSetPerfmon();

            _perfmon.TotalInvocations.Increment();

            TryGetMethodToInvoke(args, /*false, new StackTrace(),*/ method, out var methodToInvoke);
            if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer) return InvokeSoapHelper<T>(args, methodToInvoke);

            GridProcessHelper.OpenWebServerIfNotOpen();

            var instance = GetOrCreateGridServerInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            SystemLogger.Singleton.Debug("Got the instance '{0}' to execute method '{1}'", instance, method);

            return InvokeMethodToInvoke<T>(args, methodToInvoke, instance);
        }

        private async Task InvokeMethodAsync(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args
        )
            => await InvokeMethodAsync<object>(method, name, maxAttemptsToHitGridServer, hostName, isPoolable, args);
        private async Task<T> InvokeMethodAsync<T>(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args
        )
        {
            CheckAndSetPerfmon();

            _perfmon.TotalInvocations.Increment();

            TryGetMethodToInvoke(args, /*true, new StackTrace(),*/ method, out var methodToInvoke);
            if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                return await InvokeSoapHelperAsync<T>(args, methodToInvoke);

            GridProcessHelper.OpenWebServerIfNotOpen();

            var instance = GetOrCreateGridServerInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            SystemLogger.Singleton.Debug("Got the instance '{0}' to execute method '{1}'", instance, method);

            return await InvokeMethodToInvokeAsync<T>(args, methodToInvoke, instance);
        }

        private async Task<T> InvokeMethodToInvokeAsync<T>(object[] args, MethodInfo methodToInvoke, GridServerInstance instance)
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
            finally { TryCleanupInstance(instance); }
        }

        private void TryCleanupInstance(GridServerInstance instance)
        {
            if (instance.Persistent) return;
            PortAllocation.RemovePortFromCacheIfExists(instance.Port);
            lock (_instances)
                _instances.Remove(instance);
            instance.Dispose();
            BatchQueueUpArbiteredInstancesUnsafe(
#if DEBUG
                1,
                5,
                "localhost",
                false
#endif
            );
        }

        private T InvokeMethodToInvoke<T>(object[] args, MethodInfo methodToInvoke, GridServerInstance instance)
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
            finally { TryCleanupInstance(instance); }
        }


        private T InvokeSoapHelper<T>(object[] args, MethodInfo methodToInvoke)
        {
            try { _perfmon.TotalInvocationsThatHitTheSingleInstancedArbiter.Increment(); return (T)methodToInvoke.Invoke(SingleInstancedArbiter.Singleton, args); }
            catch (TargetInvocationException ex)
            {
                _perfmon.TotalInvocationsThatFailed.Increment();
                if (ex.InnerException != null)
                    throw ex.InnerException;

                throw;
            }
        }

        private async Task<T> InvokeSoapHelperAsync<T>(object[] args, MethodInfo methodToInvoke)
        {
            try
            {
                _perfmon.TotalInvocationsThatHitTheSingleInstancedArbiter.Increment();
                return await ((Task<T>)methodToInvoke.Invoke(SingleInstancedArbiter.Singleton, args)).ConfigureAwait(false);
            }
            catch (TargetInvocationException ex)
            {
                _perfmon.TotalInvocationsThatFailed.Increment();
                if (ex.InnerException != null)
                    throw ex.InnerException;

                throw;
            }
        }

        private static void TryGetMethodToInvoke(IEnumerable<object> args, /*bool isAsync, StackTrace stack,*/ string lastMethod, out MethodInfo methodToInvoke)
        {
            /*if (isAsync)
            {
                // Call stack, we want the num 5
                // 0: <InvokeMethodAsync>d__30`1.MoveNext()
                // 1: AsyncTaskMethodBuilder`1.Start[TStateMachine](TStateMachine& stateMachine)
                // 2: GridServerInstance.InvokeMethodAsync[T](Object[] args)
                // 3: <MethodName>d__40.MoveNext()
                // 4: AsyncTaskMethodBuilder`1.Start[TStateMachine](TStateMachine& stateMachine)
                // 5: GridServerInstance.MethodName()
                lastMethod = stack.GetFrame(5).GetMethod().Name;
                if (lastMethod == "InvokeMethodAsync") lastMethod = stack.GetFrame(8).GetMethod().Name;
            }
            else
            {
                lastMethod = stack.GetFrame(1).GetMethod().Name;

                // This is here incase we call the overload
                if (lastMethod == "InvokeMethod") lastMethod = stack.GetFrame(2).GetMethod().Name;
            }*/

            methodToInvoke = global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer
                ? SingleInstancedArbiter.Singleton.GetType()
                    .GetMethod(lastMethod,
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        args.Select(x => x.GetType())
                            .ToArray(),
                        null)
                : typeof(GridServerInstance).GetMethod(lastMethod,
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    args.Select(x => x.GetType())
                        .ToArray(),
                    null);

            if (methodToInvoke == null)
                throw new ApplicationException($"Unknown grid server method '{lastMethod}'.");
        }

        #endregion |Invocation Helpers|

        #region |SOAP Methods|

        public string HelloWorld() => HelloWorld(5, "localhost");
        public string HelloWorld(int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<string>(BaseClassHelloWorldMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public string HelloWorld(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<string>(BaseClassHelloWorldMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<string> HelloWorldAsync() => await HelloWorldAsync(5, "localhost");
        public async Task<string> HelloWorldAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<string>(BaseClassHelloWorldAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<string> HelloWorldAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<string>(BaseClassHelloWorldAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public string GetVersion() => GetVersion(5, "localhost");
        public string GetVersion(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<string>(BaseClassGetVersionMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public string GetVersion(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<string>(BaseClassGetVersionMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<string> GetVersionAsync() => await GetVersionAsync(5, "localhost");
        public async Task<string> GetVersionAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<string>(BaseClassGetVersionAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<string> GetVersionAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<string>(BaseClassGetVersionAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public Status GetStatus() => GetStatus(5, "localhost");
        public Status GetStatus(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<Status>(BaseClassGetStatusMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public Status GetStatus(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
             => InvokeMethod<Status>(BaseClassGetStatusMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<Status> GetStatusAsync() => await GetStatusAsync(5, "localhost");
        public async Task<Status> GetStatusAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<Status>(BaseClassGetStatusAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<Status> GetStatusAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<Status>(BaseClassGetStatusAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public LuaValue[] OpenJob(Job job, ScriptExecution script) => OpenJob(job, script, 5, "localhost");
        public LuaValue[] OpenJob(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassOpenJobMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] OpenJob(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassOpenJobMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script) => await OpenJobAsync(job, script, 5, "localhost");
        public async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<OpenJobResponse>(BaseClassOpenJobAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<OpenJobResponse> OpenJobAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<OpenJobResponse>(BaseClassOpenJobAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public LuaValue[] OpenJobEx(Job job, ScriptExecution script) => OpenJobEx(job, script, 5, "localhost");
        public LuaValue[] OpenJobEx(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassOpenJobExMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] OpenJobEx(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassOpenJobExMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script) => await OpenJobExAsync(job, script, 5, "localhost");
        public async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassOpenJobExAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<LuaValue[]> OpenJobExAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassOpenJobExAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public LuaValue[] Execute(string jobId, ScriptExecution script) => Execute(jobId, script, 5, "localhost");
        public LuaValue[] Execute(string jobId, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassExecuteMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId, script);
        public LuaValue[] Execute(string name, string jobId, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassExecuteMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId, script);

        public async Task<ExecuteResponse> ExecuteAsync(string jobId, ScriptExecution script) => await ExecuteAsync(jobId, script, 5, "localhost");
        public async Task<ExecuteResponse> ExecuteAsync(string jobId, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<ExecuteResponse>(BaseClassExecuteAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId, script);
        public async Task<ExecuteResponse> ExecuteAsync(string name, string jobId, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<ExecuteResponse>(BaseClassExecuteAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId, script);

        public LuaValue[] ExecuteEx(string jobId, ScriptExecution script) => ExecuteEx(jobId, script, 5, "localhost");
        public LuaValue[] ExecuteEx(string jobId, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassExecuteExMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId, script);
        public LuaValue[] ExecuteEx(string name, string jobId, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassExecuteExMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId, script);

        public async Task<LuaValue[]> ExecuteExAsync(string jobId, ScriptExecution script) => await ExecuteExAsync(jobId, script, 5, "localhost");
        public async Task<LuaValue[]> ExecuteExAsync(string jobId, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassExecuteExAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId, script);
        public async Task<LuaValue[]> ExecuteExAsync(string name, string jobId, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassExecuteExAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId, script);

        public void CloseJob(string jobId) => CloseJob(jobId, 5, "localhost");
        public void CloseJob(string jobId, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod(BaseClassCloseJobMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId);
        public void CloseJob(string name, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod(BaseClassCloseJobMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId);

        public async Task CloseJobAsync(string jobId) => await CloseJobAsync(jobId, 5, "localhost");
        public async Task CloseJobAsync(string jobId, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync(BaseClassCloseJobAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId);
        public async Task CloseJobAsync(string name, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync(BaseClassCloseJobAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId);

        public LuaValue[] BatchJob(Job job, ScriptExecution script) => BatchJob(job, script, 5, "localhost");
        public LuaValue[] BatchJob(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassBatchJobMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] BatchJob(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassBatchJobMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script) => await BatchJobAsync(job, script, 5, "localhost");
        public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<BatchJobResponse>(BaseClassBatchJobAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<BatchJobResponse> BatchJobAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<BatchJobResponse>(BaseClassBatchJobAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public LuaValue[] BatchJobEx(Job job, ScriptExecution script) => BatchJobEx(job, script, 5, "localhost");
        public LuaValue[] BatchJobEx(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassBatchJobExMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] BatchJobEx(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassBatchJobExMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script) => await BatchJobExAsync(job, script, 5, "localhost");
        public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassBatchJobExAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<LuaValue[]> BatchJobExAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassBatchJobExAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public double GetExpiration(string jobId) => GetExpiration(jobId, 5, "localhost");
        public double GetExpiration(string jobId, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<double>(BaseClassGetExpirationMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId);
        public double GetExpiration(string name, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<double>(BaseClassGetExpirationMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId);

        public async Task<double> GetExpirationAsync(string jobId) => await GetExpirationAsync(jobId, 5, "localhost");
        public async Task<double> GetExpirationAsync(string jobId, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<double>(BaseClassGetExpirationAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, jobId);
        public async Task<double> GetExpirationAsync(string name, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<double>(BaseClassGetExpirationAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, jobId);

        public Job[] GetAllJobs() => GetAllJobs(5, "localhost");
        public Job[] GetAllJobs(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<Job[]>(BaseClassGetAllJobsMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public Job[] GetAllJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<Job[]>(BaseClassGetAllJobsMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<GetAllJobsResponse> GetAllJobsAsync() => await GetAllJobsAsync(5, "localhost");
        public async Task<GetAllJobsResponse> GetAllJobsAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<GetAllJobsResponse>(BaseClassGetAllJobsAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<GetAllJobsResponse> GetAllJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<GetAllJobsResponse>(BaseClassGetAllJobsAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public Job[] GetAllJobsEx() => GetAllJobsEx(5, "localhost");
        public Job[] GetAllJobsEx(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<Job[]>(BaseClassGetAllJobsExMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public Job[] GetAllJobsEx(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<Job[]>(BaseClassGetAllJobsExMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<Job[]> GetAllJobsExAsync() => await GetAllJobsExAsync(5, "localhost");
        public async Task<Job[]> GetAllJobsExAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<Job[]>(BaseClassGetAllJobsExAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<Job[]> GetAllJobsExAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<Job[]>(BaseClassGetAllJobsExAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public int CloseExpiredJobs() => CloseExpiredJobs(5, "localhost");
        public int CloseExpiredJobs(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<int>(BaseClassCloseExpiredJobsMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public int CloseExpiredJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<int>(BaseClassCloseExpiredJobsMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<int> CloseExpiredJobsAsync() => await CloseExpiredJobsAsync(5, "localhost");
        public async Task<int> CloseExpiredJobsAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<int>(BaseClassCloseExpiredJobsAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<int> CloseExpiredJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<int>(BaseClassCloseExpiredJobsAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public int CloseAllJobs() => CloseAllJobs(5, "localhost");
        public int CloseAllJobs(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<int>(BaseClassCloseAllJobsMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public int CloseAllJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<int>(BaseClassCloseAllJobsMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<int> CloseAllJobsAsync() => await CloseAllJobsAsync(5, "localhost");
        public async Task<int> CloseAllJobsAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<int>(BaseClassCloseAllJobsAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<int> CloseAllJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<int>(BaseClassCloseAllJobsAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public LuaValue[] Diag(int type, string jobId) => Diag(type, jobId, 5, "localhost");
        public LuaValue[] Diag(int type, string jobId, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassDiagMethodName, null, maxAttemptsToHitGridServer, hostName, false, type, jobId);
        public LuaValue[] Diag(string name, int type, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassDiagMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobId);

        public async Task<DiagResponse> DiagAsync(int type, string jobId) => await DiagAsync(type, jobId, 5, "localhost");
        public async Task<DiagResponse> DiagAsync(int type, string jobId, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<DiagResponse>(BaseClassDiagAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, type, jobId);
        public async Task<DiagResponse> DiagAsync(string name, int type, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<DiagResponse>(BaseClassDiagAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobId);

        public LuaValue[] DiagEx(int type, string jobId) => DiagEx(type, jobId, 5, "localhost");
        public LuaValue[] DiagEx(int type, string jobId, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(BaseClassDiagExMethodName, null, maxAttemptsToHitGridServer, hostName, false, type, jobId);
        public LuaValue[] DiagEx(string name, int type, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(BaseClassDiagExMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobId);

        public async Task<LuaValue[]> DiagExAsync(int type, string jobId) => await DiagExAsync(type, jobId, 5, "localhost");

        public async Task<LuaValue[]> DiagExAsync(int type, string jobId, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassDiagExAsyncMethodName, null, maxAttemptsToHitGridServer, hostName, false, type, jobId);
        public async Task<LuaValue[]> DiagExAsync(string name, int type, string jobId, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(BaseClassDiagExAsyncMethodName, name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobId);

        #endregion |SOAP Methods|

        [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
        public class GridServerInstance : ComputeCloudServiceSoapClient, IDisposable
        {
            #region |Private Members|

            private readonly int _maxAttemptsToHitGridServer;
            private readonly bool _isPersistent;
            private int _gridServerProcessId;
            private readonly string _name;
            private bool _isAvailable;
            private readonly bool _isPoolable;
            private readonly object _availableLock = new();
            private readonly GridServerInstancePerformanceMonitor _perf;

            #endregion |Private Members|

            #region |Informative Members|

            public bool IsOpened => _gridServerProcessId != 0;
            public int ProcessId => _gridServerProcessId;
            public bool Persistent => _isPersistent;
            public string Name => _name;
            public bool IsAvailable => _isAvailable;
            public bool IsPoolable => _isPoolable;
            public int Port => Endpoint.Address.Uri.Port;

            #endregion |Informative Members|

            #region |Contructors|

            internal GridServerInstance(
                ICounterRegistry counterRegistry,
                string host,
                int port,
                string name,
                bool openProcessNow,
                int maxAttemptsToHitGridServer = 5,
                bool persistent = false,
                bool poolable = true,
                bool openNowInNewThread = false
            )
                : this(
                    counterRegistry,
                    new EndpointAddress($"http://{host}:{port}"),
                    name,
                    openProcessNow,
                    maxAttemptsToHitGridServer,
                    persistent,
                    poolable,
                    openNowInNewThread)
            { }

            protected GridServerInstance(
                ICounterRegistry counterRegistry,
                EndpointAddress remoteAddress,
                string name,
                bool openProcessNow,
                int maxAttemptsToHitGridServer = 5,
                bool persistent = false,
                bool poolable = true,
                bool openNowInNewThread = false
            )
                : base(_defaultHttpBinding, remoteAddress)
            {
                if (maxAttemptsToHitGridServer < 1) throw new ArgumentOutOfRangeException(nameof(maxAttemptsToHitGridServer));
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
                _maxAttemptsToHitGridServer = maxAttemptsToHitGridServer;
                _isPersistent = persistent;
                if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));
                _name = name;
                _isAvailable = true;
                _isPoolable = poolable;
                _perf = new GridServerInstancePerformanceMonitor(
                    counterRegistry,
                    this
                );

                if (!openProcessNow) return;


                if (openNowInNewThread)
                {
                    ThreadPool.QueueUserWorkItem(_ => TryOpen(true));
                    return;
                }
                TryOpen();
            }

            #endregion |Contructors|

            #region |LifeCycle Managment Helpers|

            private bool TryOpen(bool @unsafe = false)
            {
                int proc;
                if (@unsafe)
                    (_, proc) = GridProcessHelper.OpenGridServerInstance(Port, true);
                else
                    (_, proc) = GridProcessHelper.OpenGridServerInstance(Port);
                if (proc == 0) return false;
                _gridServerProcessId = proc;
                return true;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                SystemLogger.Singleton.LifecycleEvent("Closing instance '{0}'...", _name);
                GridProcessHelper.KillProcessByPidSafe(ProcessId);
            }

            #endregion |LifeCycle Managment Helpers|

            #region |Invocation Helpers|

            private void InvokeMethod(string method, params object[] args) => InvokeMethod<object>(method, args);
            private T InvokeMethod<T>(string method, params object[] args)
            {
                try
                {
                    _perf.TotalInvocations.Increment();

                    LockAndTryOpen();
                    TryGetInstanceMethodToInvoke(args, /*false, new StackTrace(), */method, out var methodToInvoke);

                    for (var i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        var result = WrapInvocation<T>(methodToInvoke, method, out var @continue, args);
                        if (!@continue) return result;
                    }

                    if (global::MFDLabs.Grid.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                    {
                        _perf.TotalInvocationsThatFailed.Increment();
                        throw new TimeoutException($"The command '{_name}->{method}' reached it's max attempts to give a result.");
                    }

                    return default;
                }
                finally { Unlock(); }
            }

            public void Lock()
            {
                lock (_availableLock)
                    _isAvailable = false;
            }

            private void LockAndTryOpen()
            {
                Lock();

                if (IsOpened) return;

                while (!TryOpen())
                    Thread.Sleep(1000);
            }

            private async Task InvokeMethodAsync(string method, params object[] args) => await InvokeMethodAsync<object>(method, args);
            private async Task<T> InvokeMethodAsync<T>(string method, params object[] args)
            {
                try
                {
                    LockAndTryOpen();
                    TryGetInstanceMethodToInvoke(args, /*true, new StackTrace(),*/ method, out var methodToInvoke);

                    for (var i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        var result = await WrapInvocationAsync<T>(methodToInvoke, method, args);
                        if (!EqualityComparer<T>.Default.Equals(result, default)) return result;
                    }

                    if (global::MFDLabs.Grid.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{method}' reached it's max attempts to give a result.");

                    return default;
                }
                finally { Unlock(); }
            }

            private void TryGetInstanceMethodToInvoke(IEnumerable<object> args, /*bool isAsync, StackTrace stack,*/ string lastMethod, out MethodInfo methodToInvoke)
            {
                /*if (isAsync)
                {
                    // Call stack, we want the num 5
                    // 0: <InvokeMethodAsync>d__30`1.MoveNext()
                    // 1: AsyncTaskMethodBuilder`1.Start[TStateMachine](TStateMachine& stateMachine)
                    // 2: GridServerInstance.InvokeMethodAsync[T](Object[] args)
                    // 3: <MethodName>d__40.MoveNext()
                    // 4: AsyncTaskMethodBuilder`1.Start[TStateMachine](TStateMachine& stateMachine)
                    // 5: GridServerInstance.MethodName()
                    lastMethod = stack.GetFrame(5).GetMethod().Name;
                    if (lastMethod == "InvokeMethodAsync") lastMethod = stack.GetFrame(8).GetMethod().Name;
                }
                else
                {
                    lastMethod = stack.GetFrame(1).GetMethod().Name;

                    // This is here incase we call the overload
                    if (lastMethod == "InvokeMethod") lastMethod = stack.GetFrame(2).GetMethod().Name;
                }*/

                var type = GetType();

                Type baseType = null;

                if (type == typeof(GridServerInstance)) baseType = type.BaseType;
                else if (type == typeof(LeasedGridServerInstance)) baseType = type?.BaseType?.BaseType;

                methodToInvoke = baseType?.GetMethod(lastMethod,
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        args.Select(x => x.GetType())
                            .ToArray(),
                        null);

                if (methodToInvoke == null)
                    throw new ApplicationException($"Unknown grid server method '{lastMethod}'.");

            }

            // Virtual here, because leased instance will override this to renew lease
            public virtual void Unlock()
            {
                lock (_availableLock)
                    _isAvailable = true;
            }

            private T WrapInvocation<T>(MethodInfo methodToInvoke, string lastMethod, out bool @continue, params object[] args)
            {
                @continue = true;

                try
                {
                    var returnValue = methodToInvoke.Invoke(this, args);
                    _perf.TotalInvocationsThatSucceeded.Increment();
                    @continue = false;
                    return (T)returnValue;
                }
                catch (Exception ex) { return HandleException<T>(lastMethod, ex); }
            }

            private async Task<T> WrapInvocationAsync<T>(MethodInfo methodToInvoke, string lastMethod, params object[] args)
            {
                try
                {
                    var returnValue = await ((Task<T>)methodToInvoke.Invoke(this, args)).ConfigureAwait(false);
                    _perf.TotalInvocationsThatSucceeded.Increment();
                    return returnValue;
                }
                catch (Exception ex) { return HandleException<T>(lastMethod, ex); }
            }

            private T HandleException<T>(string lastMethod, Exception ex)
            {
                _perf.TotalInvocationsThatFailed.Increment();
                if (ex is TargetInvocationException e)
                {
                    switch (e.InnerException)
                    {
                        case EndpointNotFoundException:
                            return HandleEndpointNotFoundException<T>(lastMethod);
                        case FaultException:
                        case TimeoutException:
                            throw e.InnerException;
                    }
                }

#if DEBUG || DEBUG_LOGGING_IN_PROD
                SystemLogger.Singleton.Error("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, lastMethod, ex.ToDetailedString());
#else
                SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, lastMethod, ex.Message);
#endif
                return default;
            }

            private T HandleEndpointNotFoundException<T>(string lastMethod)
            {
                SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, lastMethod);
                if (!global::MFDLabs.Grid.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    return default;

                if (!TryOpen())
                    throw new ApplicationException($"Unable to open grid server instance '{_name}'.");

                return default;
            }

            #endregion |Invocation Helpers|

            #region |SOAP Methods|

            public new string HelloWorld() => InvokeMethod<string>(BaseClassHelloWorldMethodName);
            public new async Task<string> HelloWorldAsync() => await InvokeMethodAsync<string>(BaseClassHelloWorldAsyncMethodName);

            public new string GetVersion() => InvokeMethod<string>(BaseClassGetVersionMethodName);
            public new async Task<string> GetVersionAsync() => await InvokeMethodAsync<string>(BaseClassGetVersionAsyncMethodName);

            public new Status GetStatus() => InvokeMethod<Status>(BaseClassGetStatusMethodName);
            public new async Task<Status> GetStatusAsync() => await InvokeMethodAsync<Status>(BaseClassGetStatusAsyncMethodName);

            public new LuaValue[] OpenJob(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(BaseClassOpenJobMethodName, job, script);
            public new async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<OpenJobResponse>(BaseClassOpenJobAsyncMethodName, job, script);

            public new LuaValue[] OpenJobEx(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(BaseClassOpenJobExMethodName, job, script);
            public new async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<LuaValue[]>(BaseClassOpenJobExAsyncMethodName, job, script);

            public new double RenewLease(string jobId, double expirationInSeconds) => InvokeMethod<double>(BaseClassRenewLeaseMethodName, jobId, expirationInSeconds);
            public new async Task<double> RenewLeaseAsync(string jobId, double expirationInSeconds) => await InvokeMethodAsync<double>(BaseClassRenewLeaseAsyncMethodName, jobId, expirationInSeconds);

            public new LuaValue[] Execute(string jobId, ScriptExecution script) => InvokeMethod<LuaValue[]>(BaseClassExecuteMethodName, jobId, script);
            public new async Task<ExecuteResponse> ExecuteAsync(string jobId, ScriptExecution script) => await InvokeMethodAsync<ExecuteResponse>(BaseClassExecuteAsyncMethodName, jobId, script);

            public new LuaValue[] ExecuteEx(string jobId, ScriptExecution script) => InvokeMethod<LuaValue[]>(BaseClassExecuteExMethodName, jobId, script);
            public new async Task<LuaValue[]> ExecuteExAsync(string jobId, ScriptExecution script) => await InvokeMethodAsync<LuaValue[]>(BaseClassExecuteExAsyncMethodName, jobId, script);

            public new void CloseJob(string jobId) => InvokeMethod(BaseClassCloseJobMethodName, jobId);
            public new async Task CloseJobAsync(string jobId) => await InvokeMethodAsync(BaseClassCloseJobAsyncMethodName, jobId);

            public new LuaValue[] BatchJob(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(BaseClassBatchJobMethodName, job, script);
            public new async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<BatchJobResponse>(BaseClassBatchJobAsyncMethodName, job, script);

            public new LuaValue[] BatchJobEx(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(BaseClassBatchJobExMethodName, job, script);
            public new async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<LuaValue[]>(BaseClassBatchJobExAsyncMethodName, job, script);

            public new double GetExpiration(string jobId) => InvokeMethod<double>(BaseClassGetExpirationMethodName, jobId);
            public new async Task<double> GetExpirationAsync(string jobId) => await InvokeMethodAsync<double>(BaseClassGetExpirationAsyncMethodName, jobId);

            public new Job[] GetAllJobs() => InvokeMethod<Job[]>(BaseClassGetAllJobsMethodName);
            public new async Task<GetAllJobsResponse> GetAllJobsAsync() => await InvokeMethodAsync<GetAllJobsResponse>(BaseClassGetAllJobsAsyncMethodName);

            public new Job[] GetAllJobsEx() => InvokeMethod<Job[]>(BaseClassGetAllJobsExMethodName);
            public new async Task<Job[]> GetAllJobsExAsync() => await InvokeMethodAsync<Job[]>(BaseClassGetAllJobsExAsyncMethodName);

            public new int CloseExpiredJobs() => InvokeMethod<int>(BaseClassCloseExpiredJobsMethodName);
            public new async Task<int> CloseExpiredJobsAsync() => await InvokeMethodAsync<int>(BaseClassCloseExpiredJobsAsyncMethodName);

            public new int CloseAllJobs() => InvokeMethod<int>(BaseClassCloseAllJobsMethodName);
            public new async Task<int> CloseAllJobsAsync() => await InvokeMethodAsync<int>(BaseClassCloseAllJobsAsyncMethodName);

            public new LuaValue[] Diag(int type, string jobId) => InvokeMethod<LuaValue[]>(BaseClassDiagMethodName, type, jobId);
            public new async Task<DiagResponse> DiagAsync(int type, string jobId) => await InvokeMethodAsync<DiagResponse>(BaseClassDiagAsyncMethodName, type, jobId);

            public new LuaValue[] DiagEx(int type, string jobId) => InvokeMethod<LuaValue[]>(BaseClassDiagExMethodName, type, jobId);
            public new async Task<LuaValue[]> DiagExAsync(int type, string jobId) => await InvokeMethodAsync<LuaValue[]>(BaseClassDiagExAsyncMethodName, type, jobId);

            #endregion |SOAP Methods|

            #region Auto-Generated Items

            public override string ToString()
                => $"[{(_isPersistent ? "Persistent" : "Disposable")}] [{(_isPoolable ? "Poolable" : "Non Poolable")}] Instance [http://{Endpoint.Address.Uri.Host}:{Port}], Name = {Name}, State = {(IsOpened ? "Opened" : "Closed")}";

            public override bool Equals(object obj) => obj is GridServerInstance instance &&
                                                       _maxAttemptsToHitGridServer ==
                                                       instance._maxAttemptsToHitGridServer &&
                                                       _isPersistent == instance._isPersistent &&
                                                       _name == instance._name;

            public static bool operator ==(GridServerInstance self, GridServerInstance obj) => self?.GetHashCode() == obj?.GetHashCode();
            public static bool operator !=(GridServerInstance self, GridServerInstance obj) => self?.GetHashCode() != obj?.GetHashCode();

            // auto generated
            public override int GetHashCode()
            {
                var hashCode = 1434985217;
                hashCode = hashCode * -1521134295 + _maxAttemptsToHitGridServer.GetHashCode();
                hashCode = hashCode * -1521134295 + _isPersistent.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
                return hashCode;
            }

            #endregion Auto-Generated Items
        }

        public class LeasedGridServerInstance : GridServerInstance, IDisposable
        {
            #region |Delegates|

            public delegate void OnExpired(LeasedGridServerInstance instance);

            #endregion |Delegates|

            #region |Private Members|

            public static TimeSpan DefaultLease => global::MFDLabs.Grid.Properties.Settings.Default.DefaultLeasedGridServerInstanceLease;
            private readonly TimeSpan _lease;
            private readonly IRandom _rng = RandomFactory.GetDefaultRandom();
            private bool _disposed;
            private OnExpired _onExpiredListeners = new(_ => { });
            private readonly DateTime _expiration;

            #endregion |Private Members|

            #region |Informative Members|

            public DateTime Expiration => _expiration;
            public TimeSpan Lease => _lease;
            public bool HasLease => _lease != TimeSpan.Zero;
            public bool IsExpired => _expiration.Subtract(DateTime.Now) <= TimeSpan.Zero;
            public bool IsDisposed => _disposed;
            public new bool IsAvailable => base.IsAvailable && !IsExpired && !IsDisposed;

            #endregion |Informative Members|

            #region |Constructors|

            internal LeasedGridServerInstance(
                ICounterRegistry counterRegistry,
                TimeSpan lease,
                string host,
                int port,
                string name,
                bool openProcessNow,
                int maxAttemptsToHitGridServer = 5,
                bool poolable = true,
                bool openNowInNewThread = false
            )
                : base(
                    counterRegistry,
                    host,
                    port,
                    name,
                    openProcessNow,
                    maxAttemptsToHitGridServer,
                    false,
                    poolable,
                    openNowInNewThread
                )
            {
                _lease = lease;
                _expiration = DateTime.Now.Add(lease);
            }

            #endregion |Constructors|

            #region |Leasing Helpers|

            private void ScheduleExpirationCheck()
            {
                var span = TimeSpan.FromMilliseconds((1 + 0.2 * _rng.NextDouble()) * _lease.TotalMilliseconds + 20);
                ConcurrencyService.Singleton.Delay(span, CheckExpiration);
            }
            private void CheckExpiration()
            {
                if (IsExpired)
                {
                    SystemLogger.Singleton.Warning("Instance '{0}' lease has expired, disposing...", Name);
                    Dispose();
                }
                else
                    ScheduleExpirationCheck();
            }
            public void RenewLease()
            {
                SystemLogger.Singleton.LifecycleEvent("Renewing instance '{0}' lease '{1}', current expiration '{2}'", Name, Lease, Expiration);
                ScheduleExpirationCheck();
            }
            public void SubscribeExpirationListener(OnExpired @delegate)
            {
                SystemLogger.Singleton.Warning("Subscribing expiration listener '{0}.{1}'", @delegate.Method.DeclaringType.FullName, @delegate.Method.Name);
                _onExpiredListeners += @delegate;
            }
            public void UnsubscribeExpirationListener(OnExpired @delegate)
            {
                SystemLogger.Singleton.Warning("Unsubscribing expiration listener '{0}.{1}'", @delegate.Method.DeclaringType.FullName, @delegate.Method.Name);
                _onExpiredListeners -= @delegate;
            }

            #endregion |Leasing Helpers|

            #region |Overrides|

            public override string ToString()
            {
                var old = base.ToString();
                old += $", Lease = {Lease}, Expiration = {Expiration}";
                return $"[Leased] {old}";
            }

            public override void Unlock()
            {
                base.Unlock();
                RenewLease();
            }

            public new void Dispose()
            {
                if (!_disposed)
                {
                    GC.SuppressFinalize(this);

                    _onExpiredListeners(this);
                    base.Dispose();
                    _disposed = true;
                    Singleton.RemoveInstanceFromQueue(this);
                }
            }

            #endregion |Overrides|
        }
    }
}
