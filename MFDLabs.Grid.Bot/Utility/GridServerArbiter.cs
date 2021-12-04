/*
File name: GridServerArbiter.cs
Written By: Nikita Petko, Jakob Valara, Alex Bkordan, Elias Teleski, @networking-owk
Description: A helper to arbiter grid server instances to avoid single instanced crash exploits
TODO: Cleanup a lot of the code into shared methods.
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
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    // In here check for SingleInstancedGridServer
    // if true, piggyback off SoapUtility :)

    // so what if we have 2 instances with the same name but on different ports?
    // should we queue them up regardless, or only queue them if it's not persistent
    // seems about right :)
    public sealed class GridServerArbiter : SingletonBase<GridServerArbiter>
    {
        #region |Instrumentation|

        private class GridServerArbiterPerformanceMonitor
        {
            private const string _Category = "MFDLabs.Grid.Arbiter.PerfmonV2";

            internal IRawValueCounter TotalInvocations { get; }
            internal IRawValueCounter TotalInvocationsThatSucceeded { get; }
            internal IRawValueCounter TotalInvocationsThatFailed { get; }
            internal IRawValueCounter TotalArbiteredGridServerInstancesOpened { get; }
            internal IRawValueCounter TotalPersistentArbiteredGridServerInstancesOpened { get; }
            internal IRawValueCounter TotalInvocationsThatHitTheSoapUtility { get; }

            internal GridServerArbiterPerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException("counterRegistry");

                var instance = $"{SystemGlobal.Singleton.GetMachineID()} ({SystemGlobal.Singleton.GetMachineHost()})";

                TotalInvocations = counterRegistry.GetRawValueCounter(_Category, "TotalInvocations", instance);
                TotalInvocationsThatSucceeded = counterRegistry.GetRawValueCounter(_Category, "TotalInvocationsThatSucceeded", instance);
                TotalInvocationsThatFailed = counterRegistry.GetRawValueCounter(_Category, "TotalInvocationsThatFailed", instance);
                TotalArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(_Category, "TotalArbiteredGridServerInstancesOpened", instance);
                TotalPersistentArbiteredGridServerInstancesOpened = counterRegistry.GetRawValueCounter(_Category, "TotalPersistentArbiteredGridServerInstancesOpened", instance);
                TotalInvocationsThatHitTheSoapUtility = counterRegistry.GetRawValueCounter(_Category, "TotalInvocationsThatHitTheSoapUtility", instance);
            }
        }

        #endregion |Instrumentation|

        #region |Networking Utility|

        private class NetUtility
        {
            private const string MutexPostfix = "NetUtility";

            public static int FindNextAvailablePort(int startPort)
            {
                int port = startPort;
                bool isAvailable = true;

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
        private readonly List<GridServerInstance> _instances = new List<GridServerInstance>();
        private readonly List<int> _allocatedPorts = new List<int>();
        private readonly GridServerArbiterPerformanceMonitor _perfmon = new GridServerArbiterPerformanceMonitor(PerfmonCounterRegistryProvider.Registry);

        #endregion |Private Members|

        #region |Instance Helpers|

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
            var instanceCount = _instances.Count;
            SystemLogger.Singleton.LifecycleEvent("Disposing of all grid server instances");
            lock (_instances)
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

        public bool KillInstanceByNameUnsafe(string name, string hostName = "localhost")
        {
            var instance = GetInstance(name, hostName);
            if (instance != null)
            {
                _allocatedPorts.Remove(instance.Port);
                _instances.Remove(instance);
                instance.Dispose();
                return true;
            }

            return false;
        }

        public bool KillInstanceByName(string name, string hostName = "localhost")
        {
            var instance = GetInstance(name, hostName);
            if (instance != null)
            {
                lock (_allocatedPorts)
                    _allocatedPorts.Remove(instance.Port);
                lock (_instances)
                    _instances.Remove(instance);
                instance.Dispose();
                return true;
            }

            return false;
        }

        public List<GridServerInstance> BatchQueueUpArbiteredInstances(int count = 1, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool startUp = true)
        {
            var instances = new List<GridServerInstance>();
            for (int i = 0; i < count; i++) instances.Add(QueueUpArbiteredInstance(null, maxAttemptsToHitGridServer, hostName, startUp));
            return instances;
        }

        public List<GridServerInstance> BatchQueueUpArbiteredInstancesUnsafe(int count = 1, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool startUp = true)
        {
            var instances = new List<GridServerInstance>();
            for (int i = 0; i < count; i++) instances.Add(QueueUpArbiteredInstanceUnsafe(null, maxAttemptsToHitGridServer, hostName, startUp));
            return instances;
        }

        //warning: THIS HAS ZERO THREAD SAFETY !!!
        //it also pools start up, so we may not get the arbiter back for a while!!!!!!
        public GridServerInstance QueueUpArbiteredInstanceUnsafe(string name = null, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool startUp = true, bool openNowInNewThread = true)
        {
            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();
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
            SystemLogger.Singleton.Debug("Queueing up arbitered instance '{0}' on host '{1}'", instance.Name, instance.Endpoint.Address.Uri.ToString());
            _instances.Add(instance);
            return instance;
        }

        public GridServerInstance QueueUpPersistentArbiteredInstanceUnsafe(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false, bool startUp = true, bool openNowInNewThread = true)
        {
            _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();
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
            SystemLogger.Singleton.Debug("Queueing up persistent arbitered instance '{0}' on host '{1}'", instance.Name, instance.Endpoint.Address.Uri.ToString());
            _instances.Add(instance);
            return instance;
        }

        public GridServerInstance QueueUpArbiteredInstance(string name = null, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool startUp = true, bool openNowInNewThread = false)
        {
            _perfmon.TotalArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            lock (_allocatedPorts)
                _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();
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
            SystemLogger.Singleton.Debug("Queueing up arbitered instance '{0}' on host '{1}'", instance.Name, instance.Endpoint.Address.Uri.ToString());
            lock (_instances)
                _instances.Add(instance);
            return instance;
        }
        public GridServerInstance QueueUpPersistentArbiteredInstance(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false, bool startUp = true, bool openNowInNewThread = false)
        {
            _perfmon.TotalPersistentArbiteredGridServerInstancesOpened.Increment();

            var currentAllocatedPort = _allocatedPorts.LastOrDefault();
            if (currentAllocatedPort == default) currentAllocatedPort = GridServerStartPort;
            currentAllocatedPort++;

            lock (_allocatedPorts)
                _allocatedPorts.Add(currentAllocatedPort);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();
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
            SystemLogger.Singleton.Debug("Queueing up persistent arbitered instance '{0}' on host '{1}'", instance.Name, instance.Endpoint.Address.Uri.ToString());
            lock (_instances)
                _instances.Add(instance);
            return instance;
        }

        public GridServerInstance GetInstance(string name, string hostName = "localhost")
        {
            lock (_instances)
                return (from instance in _instances where instance.Name == name && instance.Endpoint.Address.Uri.Host == hostName select instance).FirstOrDefault();
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
                return (from instance in _instances where instance.Name == name && instance.Endpoint.Address.Uri.Host == hostName && instance.Persistent == true select instance).FirstOrDefault();
        }

        public GridServerInstance GetOrCreatePersistentInstance(string name, int maxAttemptsToHitGridServer = 5, string hostName = "hostname", bool isPoolable = false, bool openNowInNewThread = false)
        {
            var instance = GetPersistentInstance(name, hostName);
            if (instance == null) return QueueUpPersistentArbiteredInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable, openNowInNewThread);
            return instance;
        }

        public GridServerInstance GetAvailableInstance()
        {
            lock (_instances)
                return (from instance in _instances where instance.IsPoolable == true && instance.IsAvailable == true select instance).FirstOrDefault();
        }

        public GridServerInstance GetOrCreateAvailableInstance(int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool openNowInNewThread = false)
        {
            var instance = GetAvailableInstance();
            if (instance == null) return QueueUpArbiteredInstance(null, maxAttemptsToHitGridServer, hostName, openNowInNewThread);
            return instance;
        }

        private GridServerInstance GetOrCreateGridServerInstance(string name, int maxAttemptsToHitGridServer, string hostName, bool isPoolable)
        {
            GridServerInstance instance;

            if (!name.IsNullOrEmpty())
                instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);
            else
                instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);
            return instance;
        }

        #endregion |Instance Helpers|

        #region |Invocation Helpers|

        private void InvokeMethod(string name, int maxAttemptsToHitGridServer, string hostName, bool isPoolable, params object[] args)
            => InvokeMethod<object>(name, maxAttemptsToHitGridServer, hostName, isPoolable, args);
        private T InvokeMethod<T>(string name, int maxAttemptsToHitGridServer, string hostName, bool isPoolable, params object[] args)
        {
            _perfmon.TotalInvocations.Increment();

            TryGetMethodToInvoke(args, false, new StackTrace(), out var methodToInvoke);
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return InvokeSoapUtility<T>(args, methodToInvoke);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateGridServerInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);
            return InvokeMethodToInvoke<T>(args, methodToInvoke, instance);
        }

        private async Task InvokeMethodAsync(string name, int maxAttemptsToHitGridServer, string hostName, bool isPoolable, params object[] args)
            => await InvokeMethodAsync<object>(name, maxAttemptsToHitGridServer, hostName, isPoolable, args);
        private async Task<T> InvokeMethodAsync<T>(string name, int maxAttemptsToHitGridServer, string hostName, bool isPoolable, params object[] args)
        {
            _perfmon.TotalInvocations.Increment();

            TryGetMethodToInvoke(args, true, new StackTrace(), out var methodToInvoke);
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await InvokeSoapUtilityAsync<T>(args, methodToInvoke);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateGridServerInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);
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
            catch (TargetInvocationException ex) { _perfmon.TotalInvocationsThatFailed.Increment(); throw ex.InnerException; }
            finally { TryCleanupInstance(instance); }
        }

        private void TryCleanupInstance(GridServerInstance instance)
        {
            if (!instance.Persistent)
            {
                lock (_allocatedPorts)
                    _allocatedPorts.Remove(instance.Port);
                lock (_instances)
                    _instances.Remove(instance);
                instance.Dispose();
            }
        }

        private T InvokeMethodToInvoke<T>(object[] args, MethodInfo methodToInvoke, GridServerInstance instance)
        {
            try
            {
                var result = (T)methodToInvoke.Invoke(instance, args);
                _perfmon.TotalInvocationsThatSucceeded.Increment();
                return result;
            }
            catch (TargetInvocationException ex) { _perfmon.TotalInvocationsThatFailed.Increment(); throw ex.InnerException; }
            finally { TryCleanupInstance(instance); }
        }


        private T InvokeSoapUtility<T>(object[] args, MethodInfo methodToInvoke)
        {
            try { _perfmon.TotalInvocationsThatHitTheSoapUtility.Increment(); return (T)methodToInvoke.Invoke(SoapUtility.Singleton, args); }
            catch (TargetInvocationException ex) { _perfmon.TotalInvocationsThatFailed.Increment(); throw ex.InnerException; }
        }

        private async Task<T> InvokeSoapUtilityAsync<T>(object[] args, MethodInfo methodToInvoke)
        {
            try { _perfmon.TotalInvocationsThatHitTheSoapUtility.Increment(); return await ((Task<T>)methodToInvoke.Invoke(SoapUtility.Singleton, args)).ConfigureAwait(false); }
            catch (TargetInvocationException ex) { _perfmon.TotalInvocationsThatFailed.Increment(); throw ex.InnerException; }
        }

        private void TryGetMethodToInvoke(object[] args, bool isAsync, StackTrace stack, out MethodInfo methodToInvoke)
        {
            string lastMethod = null;

            if (isAsync)
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
            }

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                methodToInvoke = SoapUtility.Singleton.GetType().GetMethod(lastMethod, BindingFlags.Instance | BindingFlags.Public, null, args.Select(x => x.GetType()).ToArray(), null);
            else
                methodToInvoke = typeof(GridServerInstance).GetMethod(lastMethod, BindingFlags.Instance | BindingFlags.Public, null, args.Select(x => x.GetType()).ToArray(), null);

            if (methodToInvoke == null)
                throw new ApplicationException($"Unknown grid server method '{lastMethod}'.");
        }

        #endregion |Invocation Helpers|

        #region |SOAP Methods|

        public string HelloWorld() => HelloWorld(5, "localhost");
        public string HelloWorld(int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<string>(null, maxAttemptsToHitGridServer, hostName, false);
        public string HelloWorld(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<string>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<string> HelloWorldAsync() => await HelloWorldAsync(5, "localhost");
        public async Task<string> HelloWorldAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<string>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<string> HelloWorldAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<string>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public string GetVersion() => GetVersion(5, "localhost");
        public string GetVersion(int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<string>(null, maxAttemptsToHitGridServer, hostName, false);
        public string GetVersion(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<string>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<string> GetVersionAsync() => await GetVersionAsync(5, "localhost");
        public async Task<string> GetVersionAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<string>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<string> GetVersionAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<string>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public Status GetStatus() => GetStatus(5, "localhost");
        public Status GetStatus(int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<Status>(null, maxAttemptsToHitGridServer, hostName, false);
        public Status GetStatus(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
             => InvokeMethod<Status>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<Status> GetStatusAsync() => await GetStatusAsync(5, "localhost");
        public async Task<Status> GetStatusAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<Status>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<Status> GetStatusAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<Status>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public LuaValue[] OpenJob(Job job, ScriptExecution script) => OpenJob(job, script, 5, "localhost");
        public LuaValue[] OpenJob(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] OpenJob(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script) => await OpenJobAsync(job, script, 5, "localhost");
        public async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<OpenJobResponse>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<OpenJobResponse> OpenJobAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<OpenJobResponse>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public LuaValue[] OpenJobEx(Job job, ScriptExecution script) => OpenJobEx(job, script, 5, "localhost");
        public LuaValue[] OpenJobEx(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] OpenJobEx(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script) => await OpenJobExAsync(job, script, 5, "localhost");
        public async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<LuaValue[]> OpenJobExAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public LuaValue[] Execute(string jobID, ScriptExecution script) => Execute(jobID, script, 5, "localhost");
        public LuaValue[] Execute(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, jobID, script);
        public LuaValue[] Execute(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID, script);

        public async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script) => await ExecuteAsync(jobID, script, 5, "localhost");
        public async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<ExecuteResponse>(null, maxAttemptsToHitGridServer, hostName, false, jobID, script);
        public async Task<ExecuteResponse> ExecuteAsync(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<ExecuteResponse>(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID, script);

        public LuaValue[] ExecuteEx(string jobID, ScriptExecution script) => ExecuteEx(jobID, script, 5, "localhost");
        public LuaValue[] ExecuteEx(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, jobID, script);
        public LuaValue[] ExecuteEx(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID, script);

        public async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script) => await ExecuteExAsync(jobID, script, 5, "localhost");
        public async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, jobID, script);
        public async Task<LuaValue[]> ExecuteExAsync(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID, script);

        public void CloseJob(string jobID) => CloseJob(jobID, 5, "localhost");
        public void CloseJob(string jobID, int maxAttemptsToHitGridServer, string hostName) => InvokeMethod(null, maxAttemptsToHitGridServer, hostName, false, jobID);
        public void CloseJob(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID);

        public async Task CloseJobAsync(string jobID) => await CloseJobAsync(jobID, 5, "localhost");
        public async Task CloseJobAsync(string jobID, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync(null, maxAttemptsToHitGridServer, hostName, false, jobID);
        public async Task CloseJobAsync(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID);

        public LuaValue[] BatchJob(Job job, ScriptExecution script) => BatchJob(job, script, 5, "localhost");
        public LuaValue[] BatchJob(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] BatchJob(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script) => await BatchJobAsync(job, script, 5, "localhost");
        public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<BatchJobResponse>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<BatchJobResponse> BatchJobAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<BatchJobResponse>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public LuaValue[] BatchJobEx(Job job, ScriptExecution script) => BatchJobEx(job, script, 5, "localhost");
        public LuaValue[] BatchJobEx(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public LuaValue[] BatchJobEx(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script) => await BatchJobExAsync(job, script, 5, "localhost");
        public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, job, script);
        public async Task<LuaValue[]> BatchJobExAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, job, script);

        public double GetExpiration(string jobID) => GetExpiration(jobID, 5, "localhost");
        public double GetExpiration(string jobID, int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<double>(null, maxAttemptsToHitGridServer, hostName, false, jobID);
        public double GetExpiration(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<double>(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID);

        public async Task<double> GetExpirationAsync(string jobID) => await GetExpirationAsync(jobID, 5, "localhost");
        public async Task<double> GetExpirationAsync(string jobID, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<double>(null, maxAttemptsToHitGridServer, hostName, false, jobID);
        public async Task<double> GetExpirationAsync(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<double>(name, maxAttemptsToHitGridServer, hostName, isPoolable, jobID);

        public Job[] GetAllJobs() => GetAllJobs(5, "localhost");
        public Job[] GetAllJobs(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<Job[]>(null, maxAttemptsToHitGridServer, hostName, false);
        public Job[] GetAllJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<Job[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<GetAllJobsResponse> GetAllJobsAsync() => await GetAllJobsAsync(5, "localhost");
        public async Task<GetAllJobsResponse> GetAllJobsAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<GetAllJobsResponse>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<GetAllJobsResponse> GetAllJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<GetAllJobsResponse>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public Job[] GetAllJobsEx() => GetAllJobsEx(5, "localhost");
        public Job[] GetAllJobsEx(int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<Job[]>(null, maxAttemptsToHitGridServer, hostName, false);
        public Job[] GetAllJobsEx(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<Job[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<Job[]> GetAllJobsExAsync() => await GetAllJobsExAsync(5, "localhost");
        public async Task<Job[]> GetAllJobsExAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<Job[]>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<Job[]> GetAllJobsExAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<Job[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public int CloseExpiredJobs() => CloseExpiredJobs(5, "localhost");
        public int CloseExpiredJobs(int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<int>(null, maxAttemptsToHitGridServer, hostName, false);
        public int CloseExpiredJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<int>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<int> CloseExpiredJobsAsync() => await CloseExpiredJobsAsync(5, "localhost");
        public async Task<int> CloseExpiredJobsAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<int>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<int> CloseExpiredJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<int>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public int CloseAllJobs() => CloseAllJobs(5, "localhost");
        public int CloseAllJobs(int maxAttemptsToHitGridServer, string hostName) => InvokeMethod<int>(null, maxAttemptsToHitGridServer, hostName, false);
        public int CloseAllJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<int>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public async Task<int> CloseAllJobsAsync() => await CloseAllJobsAsync(5, "localhost");
        public async Task<int> CloseAllJobsAsync(int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<int>(null, maxAttemptsToHitGridServer, hostName, false);
        public async Task<int> CloseAllJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<int>(name, maxAttemptsToHitGridServer, hostName, isPoolable);

        public LuaValue[] Diag(int type, string jobID) => Diag(type, jobID, 5, "localhost");
        public LuaValue[] Diag(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, type, jobID);
        public LuaValue[] Diag(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobID);

        public async Task<DiagResponse> DiagAsync(int type, string jobID) => await DiagAsync(type, jobID, 5, "localhost");
        public async Task<DiagResponse> DiagAsync(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<DiagResponse>(null, maxAttemptsToHitGridServer, hostName, false, type, jobID);
        public async Task<DiagResponse> DiagAsync(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<DiagResponse>(name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobID);

        public LuaValue[] DiagEx(int type, string jobID) => DiagEx(type, jobID, 5, "localhost");
        public LuaValue[] DiagEx(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
            => InvokeMethod<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, type, jobID);
        public LuaValue[] DiagEx(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => InvokeMethod<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobID);

        public async Task<LuaValue[]> DiagExAsync(int type, string jobID) => await DiagExAsync(type, jobID, 5, "localhost");

        public async Task<LuaValue[]> DiagExAsync(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
            => await InvokeMethodAsync<LuaValue[]>(null, maxAttemptsToHitGridServer, hostName, false, type, jobID);
        public async Task<LuaValue[]> DiagExAsync(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
            => await InvokeMethodAsync<LuaValue[]>(name, maxAttemptsToHitGridServer, hostName, isPoolable, type, jobID);

        #endregion |SOAP Methods|

        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        public sealed class GridServerInstance : ComputeCloudServiceSoapClient, IDisposable
        {
            #region |Private Members|

            private readonly int _maxAttemptsToHitGridServer;
            private readonly bool _isPersistent;
            private int _gridServerProcessID;
            private TimeSpan _totalTimeToOpenInstance;
            private readonly string _name;
            private bool _isAvailable;
            private readonly bool _isPoolable;
            private readonly object _availableLock = new object();
            private static readonly Binding DefaultHTTPBinding =
                new BasicHttpBinding(BasicHttpSecurityMode.None)
                {
                    MaxReceivedMessageSize = int.MaxValue,
                    SendTimeout = global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServiceTimeout
                };

            #endregion |Private Members|

            #region |Informative Members|

            public bool IsOpened { get { return _gridServerProcessID != 0; } }
            public int ProcessID { get { return _gridServerProcessID; } }
            public bool Persistent { get { return _isPersistent; } }
            public string Name { get { return _name; } }
            public bool IsAvailable { get { return _isAvailable; } }
            public bool IsPoolable { get { return _isPoolable; } }
            public int Port { get { return Endpoint.Address.Uri.Port; } }

            #endregion |Informative Members|

            #region |Contructors|

            private GridServerInstance() { }
            internal GridServerInstance(string host, int port, string name, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : this(new EndpointAddress($"http://{host}:{port}"), name, maxAttemptsToHitGridServer, persistent, poolable, openNowInNewThread)
            { }
            internal GridServerInstance(EndpointAddress remoteAddress, string name, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : this(remoteAddress, name, false, maxAttemptsToHitGridServer, persistent, poolable, openNowInNewThread)
            {
            }
            internal GridServerInstance(string host, int port, string name, bool openProcessNow, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : this(new EndpointAddress($"http://{host}:{port}"), name, openProcessNow, maxAttemptsToHitGridServer, persistent, poolable, openNowInNewThread)
            { }
            internal GridServerInstance(EndpointAddress remoteAddress, string name, bool openProcessNow, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : base(DefaultHTTPBinding, remoteAddress)
            {
                if (maxAttemptsToHitGridServer < 1) throw new ArgumentOutOfRangeException("maxAttemptsToHitGridServer");
                _maxAttemptsToHitGridServer = maxAttemptsToHitGridServer;
                _isPersistent = persistent;
                if (name.IsNullOrEmpty())
                    throw new ArgumentNullException("name");
                _name = name;
                _isAvailable = true;
                _isPoolable = poolable;
                if (openProcessNow)
                {
                    if (openNowInNewThread)
                    {
                        ThreadPool.QueueUserWorkItem((s) => TryOpen(true));
                        return;
                    }
                    TryOpen();
                }
            }

            #endregion |Contructors|

            #region |LifeCycle Managment Helpers|

            public bool TryOpen(bool @unsafe = false)
            {
                TimeSpan tto;
                int proc;
                if (@unsafe)
                    (tto, proc) = SystemUtility.Singleton.OpenGridServerInstance(Port, true);
                else
                    (tto, proc) = SystemUtility.Singleton.OpenGridServerInstance(Port);
                if (proc == 0) return false;
                _totalTimeToOpenInstance = tto;
                _gridServerProcessID = proc;
                return true;
            }

            public void Dispose()
            {
                SystemLogger.Singleton.LifecycleEvent("Closing instance '{0}'...", _name);
                SystemUtility.Singleton.KillProcessByPIDSafe(ProcessID);
            }

            #endregion |LifeCycle Managment Helpers|

            #region |Invocation Helpers|

            private void InvokeMethod(params object[] args) => InvokeMethod<object>(args);
            private T InvokeMethod<T>(params object[] args)
            {
                try
                {
                    LockAndTryOpen();
                    TryGetMethodToInvoke(args, false, new StackTrace(), out string lastMethod, out var methodToInvoke);

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        var result = WrapInvocation<T>(methodToInvoke, lastMethod, out var @continue, args);
                        if (!@continue) return result;
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{lastMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally { Unlock(); }
            }

            private void LockAndTryOpen()
            {
                lock (_availableLock)
                    _isAvailable = false;

                if (!IsOpened) if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
            }

            private async Task InvokeMethodAsync(params object[] args) => await InvokeMethodAsync<object>(args);
            private async Task<T> InvokeMethodAsync<T>(params object[] args)
            {
                try
                {
                    LockAndTryOpen();
                    TryGetMethodToInvoke(args, true, new StackTrace(), out string lastMethod, out var methodToInvoke);

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        var result = await WrapInvocationAsync<T>(methodToInvoke, lastMethod, args);
                        if (!EqualityComparer<T>.Default.Equals(result, default)) return result;
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{lastMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally { Unlock(); }
            }

            private void TryGetMethodToInvoke(object[] args, bool isAsync, StackTrace stack, out string lastMethod, out MethodInfo methodToInvoke)
            {
                if (isAsync)
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
                }

                methodToInvoke = GetType().BaseType.GetMethod(lastMethod, BindingFlags.Instance | BindingFlags.Public, null, args.Select(x => x.GetType()).ToArray(), null);

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
                    if (e.InnerException is EndpointNotFoundException) return HandleEndpointNotFoundException<T>(lastMethod);
                    if (e.InnerException is FaultException || e.InnerException is TimeoutException) throw e.InnerException;
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
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                return default;
            }

            #endregion |Invocation Helpers|

            #region |SOAP Methods|

            public new string HelloWorld() => InvokeMethod<string>();
            public new async Task<string> HelloWorldAsync() => await InvokeMethodAsync<string>();

            public new string GetVersion() => InvokeMethod<string>();
            public new async Task<string> GetVersionAsync() => await InvokeMethodAsync<string>();

            public new Status GetStatus() => InvokeMethod<Status>();
            public new async Task<Status> GetStatusAsync() => await InvokeMethodAsync<Status>();

            public new LuaValue[] OpenJob(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(job, script);
            public new async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<OpenJobResponse>(job, script);

            public new LuaValue[] OpenJobEx(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(job, script);
            public new async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<LuaValue[]>(job, script);

            public new double RenewLease(string jobID, double expirationInSeconds) => InvokeMethod<double>(jobID, expirationInSeconds);
            public new async Task<double> RenewLeaseAsync(string jobID, double expirationInSeconds) => await InvokeMethodAsync<double>(jobID, expirationInSeconds);

            public new LuaValue[] Execute(string jobID, ScriptExecution script) => InvokeMethod<LuaValue[]>(jobID, script);
            public new async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script) => await InvokeMethodAsync<ExecuteResponse>(jobID, script);

            public new LuaValue[] ExecuteEx(string jobID, ScriptExecution script) => InvokeMethod<LuaValue[]>(jobID, script);
            public new async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script) => await InvokeMethodAsync<LuaValue[]>(jobID, script);

            public new void CloseJob(string jobID) => InvokeMethod(jobID);
            public new async Task CloseJobAsync(string jobID) => await InvokeMethodAsync(jobID);

            public new LuaValue[] BatchJob(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(job, script);
            public new async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<BatchJobResponse>(job, script);

            public new LuaValue[] BatchJobEx(Job job, ScriptExecution script) => InvokeMethod<LuaValue[]>(job, script);
            public new async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script) => await InvokeMethodAsync<LuaValue[]>(job, script);

            public new double GetExpiration(string jobID) => InvokeMethod<double>(jobID);
            public new async Task<double> GetExpirationAsync(string jobID) => await InvokeMethodAsync<double>(jobID);

            public new Job[] GetAllJobs() => InvokeMethod<Job[]>();
            public new async Task<GetAllJobsResponse> GetAllJobsAsync() => await InvokeMethodAsync<GetAllJobsResponse>();

            public new Job[] GetAllJobsEx() => InvokeMethod<Job[]>();
            public new async Task<Job[]> GetAllJobsExAsync() => await InvokeMethodAsync<Job[]>();

            public new int CloseExpiredJobs() => InvokeMethod<int>();
            public new async Task<int> CloseExpiredJobsAsync() => await InvokeMethodAsync<int>();

            public new int CloseAllJobs() => InvokeMethod<int>();
            public new async Task<int> CloseAllJobsAsync() => await InvokeMethodAsync<int>();

            public new LuaValue[] Diag(int type, string jobID) => InvokeMethod<LuaValue[]>(type, jobID);
            public new async Task<DiagResponse> DiagAsync(int type, string jobID) => await InvokeMethodAsync<DiagResponse>(type, jobID);

            public new LuaValue[] DiagEx(int type, string jobID) => InvokeMethod<LuaValue[]>(type, jobID);
            public new async Task<LuaValue[]> DiagExAsync(int type, string jobID) => await InvokeMethodAsync<LuaValue[]>(type, jobID);

            #endregion |SOAP Methods|

            #region Auto-Generated Items

            private string GetDebuggerDisplay()
                => $"[{(_isPersistent ? "Persistent" : "Disposable")}] [{(_isPoolable ? "Poolable" : "Non Poolable")}] Instance [http://{_name}:{Port}], State = {(IsOpened ? "Opened" : "Closed")}";

            public override bool Equals(object obj) => obj is GridServerInstance instance && _maxAttemptsToHitGridServer == instance._maxAttemptsToHitGridServer && _isPersistent == instance._isPersistent && _name == instance._name;

            // auto generated
            public override int GetHashCode()
            {
                int hashCode = 1434985217;
                hashCode = hashCode * -1521134295 + _maxAttemptsToHitGridServer.GetHashCode();
                hashCode = hashCode * -1521134295 + _isPersistent.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
                return hashCode;
            }

            #endregion Auto-Generated Items
        }
    }
}
