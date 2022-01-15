/*
File name: GridServerArbiter.cs
Written By: Nikita Petko, Jakob Valara, Alex Bkordan, Elias Teleski, @networking-owk
Description: A helper to arbiter grid server instances to avoid single instanced crash exploits
*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

#if DEBUG
using MFDLabs.ErrorHandling.Extensions;
#endif

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace MFDLabs.Grid.Bot.Utility
{
    // In here check for SingleInstancedGridServer
    // if true, piggyback off SoapUtility :)

    // so what if we have 2 instances with the same name but on different ports?
    // should we queue them up regardless, or only queue them if it's not persistent
    // seems about right :)
    public sealed class GridServerArbiter : SingletonBase<GridServerArbiter>
    {
        #region |Setup|

        private static Binding _defaultHttpBinding;

        public static void SetDefaultHttpBinding(Binding binding) => _defaultHttpBinding = binding;
        
        public void SetupPool() 
            => BatchQueueUpArbiteredInstancesUnsafe(DefaultPoolSize, 5, "localhost", false);

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
        private const int DefaultPoolSize = 25;

        #endregion |Constants|

        #region |Instrumentation|

        private class GridServerArbiterPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.Arbiter.PerfmonV2";

            internal IRawValueCounter TotalInvocations { get; }
            internal IRawValueCounter TotalInvocationsThatSucceeded { get; }
            internal IRawValueCounter TotalInvocationsThatFailed { get; }
            internal IRawValueCounter TotalArbiteredGridServerInstancesOpened { get; }
            internal IRawValueCounter TotalPersistentArbiteredGridServerInstancesOpened { get; }
            internal IRawValueCounter TotalInvocationsThatHitTheSoapUtility { get; }

            internal GridServerArbiterPerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalInvocations = counterRegistry.GetRawValueCounter(Category, "TotalInvocations", instance);
                TotalInvocationsThatSucceeded = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatSucceeded", instance);
                TotalInvocationsThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatFailed", instance);
                TotalArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(Category, "TotalArbiteredGridServerInstancesOpened", instance);
                TotalPersistentArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(Category, "TotalPersistentArbiteredGridServerInstancesOpened", instance);
                TotalInvocationsThatHitTheSoapUtility = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatHitTheSoapUtility", instance);
            }
        }

        #endregion |Instrumentation|

        #region |Networking Utility|

        private static class NetUtility
        {
            private const string MutexPostfix = "NetUtility";

            public static int FindNextAvailablePort(int startPort)
            {
                var port = startPort;
                var isAvailable = true;

                var mutex = new System.Threading.Mutex(false, string.Concat("Global/", MutexPostfix));
                mutex.WaitOne();
                try
                {
                    System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties =
                        System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                    System.Net.IPEndPoint[] endPoints =
                        ipGlobalProperties.GetActiveTcpListeners();

                    do
                    {
                        if (!isAvailable)
                        {
                            port++;
                            isAvailable = true;
                        }

                        foreach (System.Net.IPEndPoint endPoint in endPoints)
                        {
                            if (endPoint.Port != port) continue;
                            isAvailable = false;
                            break;
                        }

                    } while (!isAvailable && port < System.Net.IPEndPoint.MaxPort);

                    if (!isAvailable)
                        throw new Exception("NoAvailablePortsInRangeException");

                    return port;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        #endregion |Networking Utility|

        #region |Private Members|

        private const int GridServerStartPort = 47999;
        private readonly List<GridServerInstance> _instances = new();
        private readonly List<int> _allocatedPorts = new();
        private readonly GridServerArbiterPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion |Private Members|

        #region |Instance Helpers|

        public IReadOnlyCollection<GridServerInstance> GetAllInstances()
        {
            lock (_instances)
                return _instances.ToImmutableArray();
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public int KillAllOpenInstancesUnsafe()
        {
            var instanceCount = _instances.Count;
            SystemLogger.Singleton.LifecycleEvent("Disposing of all grid server instances");
            foreach (var instance in _instances.ToArray())
            {
                SystemLogger.Singleton.LifecycleEvent("Disposing of grid server instance: {0}", instance.Name);
                _allocatedPorts.Remove(instance.Port);
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
                    SystemLogger.Singleton.LifecycleEvent("Disposing of grid server instance: {0}", instance.Name);
                    lock (_allocatedPorts)
                        _allocatedPorts.Remove(instance.Port);
                    _instances.Remove(instance);
                    instance.Dispose();
                }

                return instanceCount;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public bool KillInstanceByNameUnsafe(string name, string hostName = "localhost")
        {
            var instance = GetInstance(name, hostName);
            if (instance == null) return false;
            
            _allocatedPorts.Remove(instance.Port);
            _instances.Remove(instance);
            instance.Dispose();
            return true;
        }

        public bool KillInstanceByName(string name, string hostName = "localhost")
        {
            var instance = GetInstance(name, hostName);
            if (instance == null) return false;
            
            lock (_allocatedPorts)
                _allocatedPorts.Remove(instance.Port);
            lock (_instances)
                _instances.Remove(instance);
            instance.Dispose();
            return true;
        }

        public List<GridServerInstance> BatchQueueUpArbiteredInstances(int count = 1,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true)
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

        public List<GridServerInstance> BatchQueueUpArbiteredInstancesUnsafe(int count = 1,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true)
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

        //warning: THIS HAS ZERO THREAD SAFETY !!!
        //it also pools start up, so we may not get the arbiter back for a while!!!!!!
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public GridServerInstance QueueUpArbiteredInstanceUnsafe(string name = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true,
            bool openNowInNewThread = true)
        {
            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                hostName,
                NetUtility.FindNextAvailablePort(currentAllocatedPort),
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

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public GridServerInstance QueueUpPersistentArbiteredInstanceUnsafe(string name,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool isPoolable = false,
            bool startUp = true,
            bool openNowInNewThread = true)
        {
            _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                hostName,
                NetUtility.FindNextAvailablePort(currentAllocatedPort),
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

        public GridServerInstance QueueUpArbiteredInstance(string name = null,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool startUp = true,
            bool openNowInNewThread = false)
        {
            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            lock (_allocatedPorts)
                _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                hostName,
                NetUtility.FindNextAvailablePort(currentAllocatedPort),
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
        public GridServerInstance QueueUpPersistentArbiteredInstance(string name,
            int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool isPoolable = false,
            bool startUp = true,
            bool openNowInNewThread = false)
        {
            _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            lock (_allocatedPorts)
                _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.OpenWebServerIfNotOpen();
            var instance = new GridServerInstance(
                hostName,
                NetUtility.FindNextAvailablePort(currentAllocatedPort),
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
            if (instance == null) return QueueUpArbiteredInstance(name, maxAttemptsToHitGridServer, hostName);
            return instance;
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

        public GridServerInstance GetOrCreateAvailableInstance(int maxAttemptsToHitGridServer = 5,
            string hostName = "localhost",
            bool openNowInNewThread = false)
        {
            var instance = GetAvailableInstance();
            return instance ?? QueueUpArbiteredInstance(null, maxAttemptsToHitGridServer, hostName, openNowInNewThread);
        }

        private GridServerInstance GetOrCreateGridServerInstance(string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable)
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
            params object[] args)
            => InvokeMethod<object>(method, name, maxAttemptsToHitGridServer, hostName, isPoolable, args);
        private T InvokeMethod<T>(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args)
        {
            _perfmon.TotalInvocations.Increment();

            TryGetMethodToInvoke(args, /*false, new StackTrace(),*/ method, out var methodToInvoke);
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return InvokeSoapUtility<T>(args, methodToInvoke);

            SystemUtility.OpenWebServerIfNotOpen();

            var instance = GetOrCreateGridServerInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);
            
#if DEBUG
            SystemLogger.Singleton.Debug("Got the instance '{0}' to execute method '{1}'", instance, method);
#endif
            
            return InvokeMethodToInvoke<T>(args, methodToInvoke, instance);
        }

        private async Task InvokeMethodAsync(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args)
            => await InvokeMethodAsync<object>(method, name, maxAttemptsToHitGridServer, hostName, isPoolable, args);
        private async Task<T> InvokeMethodAsync<T>(
            string method,
            string name,
            int maxAttemptsToHitGridServer,
            string hostName,
            bool isPoolable,
            params object[] args)
        {
            _perfmon.TotalInvocations.Increment();

            TryGetMethodToInvoke(args, /*true, new StackTrace(),*/ method, out var methodToInvoke);
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) 
                return await InvokeSoapUtilityAsync<T>(args, methodToInvoke);

            SystemUtility.OpenWebServerIfNotOpen();

            var instance = GetOrCreateGridServerInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);
            
#if DEBUG
            SystemLogger.Singleton.Debug("Got the instance '{0}' to execute method '{1}'", instance, method);
#endif
            
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
            lock (_allocatedPorts)
                _allocatedPorts.Remove(instance.Port);
            lock (_instances)
                _instances.Remove(instance);
            instance.Dispose();
            BatchQueueUpArbiteredInstancesUnsafe(1, 5, "localhost", false);
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


        private T InvokeSoapUtility<T>(object[] args, MethodInfo methodToInvoke)
        {
            try { _perfmon.TotalInvocationsThatHitTheSoapUtility.Increment(); return (T)methodToInvoke.Invoke(SoapUtility.Singleton, args); }
            catch (TargetInvocationException ex)
            {
                _perfmon.TotalInvocationsThatFailed.Increment();
                if (ex.InnerException != null) 
                    throw ex.InnerException;

                throw;
            }
        }

        private async Task<T> InvokeSoapUtilityAsync<T>(object[] args, MethodInfo methodToInvoke)
        {
            try
            {
                _perfmon.TotalInvocationsThatHitTheSoapUtility.Increment();
                return await ((Task<T>) methodToInvoke.Invoke(SoapUtility.Singleton, args)).ConfigureAwait(false);
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

            methodToInvoke = global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer
                ? SoapUtility.Singleton.GetType()
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

        [DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
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

            internal GridServerInstance(string host,
                int port,
                string name,
                bool openProcessNow,
                int maxAttemptsToHitGridServer = 5,
                bool persistent = false,
                bool poolable = true,
                bool openNowInNewThread = false)
                : this(new EndpointAddress($"http://{host}:{port}"),
                    name,
                    openProcessNow,
                    maxAttemptsToHitGridServer,
                    persistent,
                    poolable,
                    openNowInNewThread)
            { }

            private GridServerInstance(EndpointAddress remoteAddress,
                string name,
                bool openProcessNow,
                int maxAttemptsToHitGridServer = 5,
                bool persistent = false,
                bool poolable = true,
                bool openNowInNewThread = false)
                : base(_defaultHttpBinding, remoteAddress)
            {
                if (maxAttemptsToHitGridServer < 1) 
                    throw new ArgumentOutOfRangeException(nameof(maxAttemptsToHitGridServer));
                _maxAttemptsToHitGridServer = maxAttemptsToHitGridServer;
                _isPersistent = persistent;
                if (name.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(name));
                _name = name;
                _isAvailable = true;
                _isPoolable = poolable;
                
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
                    (_, proc) = SystemUtility.OpenGridServerInstance(Port, true);
                else
                    (_, proc) = SystemUtility.OpenGridServerInstance(Port);
                if (proc == 0) return false;
                _gridServerProcessId = proc;
                return true;
            }

            public void Dispose()
            {
                SystemLogger.Singleton.LifecycleEvent("Closing instance '{0}'...", _name);
                SystemUtility.KillProcessByPidSafe(ProcessId);
            }

            #endregion |LifeCycle Managment Helpers|

            #region |Invocation Helpers|

            private void InvokeMethod(string method, params object[] args) => InvokeMethod<object>(method, args);
            private T InvokeMethod<T>(string method, params object[] args)
            {
                try
                {
                    LockAndTryOpen();
                    TryGetInstanceMethodToInvoke(args, /*false, new StackTrace(), */method, out var methodToInvoke);

                    for (var i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        var result = WrapInvocation<T>(methodToInvoke, method, out var @continue, args);
                        if (!@continue) return result;
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{method}' reached it's max attempts to give a result.");

                    return default;
                }
                finally { Unlock(); }
            }

            private void LockAndTryOpen()
            {
                lock (_availableLock)
                    _isAvailable = false;

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

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
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

                methodToInvoke = GetType()
                    .BaseType?.GetMethod(lastMethod,
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        args.Select(x => x.GetType())
                            .ToArray(),
                        null);

                if (methodToInvoke == null)
                    throw new ApplicationException($"Unknown grid server method '{lastMethod}'.");

            }

            private void Unlock()
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
                    return returnValue;
                }
                catch (Exception ex) { return HandleException<T>(lastMethod, ex); }
            }

            private T HandleException<T>(string lastMethod, Exception ex)
            {
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

#if DEBUG
                SystemLogger.Singleton.Error("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, lastMethod, ex.ToDetailedString());
#else
                SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, lastMethod, ex.Message);
#endif
                return default;
            }

            private T HandleEndpointNotFoundException<T>(string lastMethod)
            {
                SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, lastMethod);
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
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
                => $"[{(_isPersistent ? "Persistent" : "Disposable")}] [{(_isPoolable ? "Poolable" : "Non Poolable")}] Instance [http://{Endpoint.Address.Uri.Host}:{Port}], State = {(IsOpened ? "Opened" : "Closed")}";

            public override bool Equals(object obj) => obj is GridServerInstance instance && _maxAttemptsToHitGridServer == instance._maxAttemptsToHitGridServer && _isPersistent == instance._isPersistent && _name == instance._name;

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
    }
}
