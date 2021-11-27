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
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    // In here check for SingleInstancedGridServer
    // if true, piggy off SoapUtility :)

    // so what if we have 2 instances with the same name but on different ports?
    // should we queue them up regardless, or only queue them if it's not persistent
    // seems about right :)
    public sealed class GridServerArbiter : SingletonBase<GridServerArbiter>
    {
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

        private const int GridServerStartPort = 47999;
        private readonly List<GridServerInstance> _instances = new List<GridServerInstance>();
        private readonly List<int> _allocatedPorts = new List<int>();
        //private readonly ICounterRegistry _counterRegistry = StaticCounterRegistry.Instance;

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

        public string HelloWorld()
        {
            return HelloWorld(5, "localhost");
        }

        public string HelloWorld(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.HelloWorld();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.HelloWorld();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public string HelloWorld(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.HelloWorld();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.HelloWorld();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<string> HelloWorldAsync()
        {
            return await HelloWorldAsync(5, "localhost");
        }

        public async Task<string> HelloWorldAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.HelloWorldAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.HelloWorldAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<string> HelloWorldAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.HelloWorldAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.HelloWorldAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public string GetVersion()
        {
            return GetVersion(5, "localhost");
        }

        public string GetVersion(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetVersion();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.GetVersion();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public string GetVersion(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetVersion();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.GetVersion();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<string> GetVersionAsync()
        {
            return await GetVersionAsync(5, "localhost");
        }

        public async Task<string> GetVersionAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetVersionAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.GetVersionAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<string> GetVersionAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetVersionAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.GetVersionAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public Status GetStatus()
        {
            return GetStatus(5, "localhost");
        }

        public Status GetStatus(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetStatus();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.GetStatus();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public Status GetStatus(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetStatus();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.GetStatus();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<Status> GetStatusAsync()
        {
            return await GetStatusAsync(5, "localhost");
        }

        public async Task<Status> GetStatusAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetStatusAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.GetStatusAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<Status> GetStatusAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetStatusAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.GetStatusAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] OpenJob(Job job, ScriptExecution script)
        {
            return OpenJob(job, script, 5, "localhost");
        }

        public LuaValue[] OpenJob(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.OpenJob(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.OpenJob(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] OpenJob(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.OpenJob(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.OpenJob(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script)
        {
            return await OpenJobAsync(job, script, 5, "localhost");
        }

        public async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.OpenJobAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.OpenJobAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<OpenJobResponse> OpenJobAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.OpenJobAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.OpenJobAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] OpenJobEx(Job job, ScriptExecution script)
        {
            return OpenJobEx(job, script, 5, "localhost");
        }

        public LuaValue[] OpenJobEx(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.OpenJobEx(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.OpenJobEx(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] OpenJobEx(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.OpenJobEx(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.OpenJobEx(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script)
        {
            return await OpenJobExAsync(job, script, 5, "localhost");
        }

        public async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.OpenJobExAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.OpenJobExAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> OpenJobExAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.OpenJobExAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.OpenJobExAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] Execute(string jobID, ScriptExecution script)
        {
            return Execute(jobID, script, 5, "localhost");
        }

        public LuaValue[] Execute(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.Execute(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.Execute(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] Execute(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.Execute(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.Execute(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script)
        {
            return await ExecuteAsync(jobID, script, 5, "localhost");
        }

        public async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.ExecuteAsync(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.ExecuteAsync(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<ExecuteResponse> ExecuteAsync(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.ExecuteAsync(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.ExecuteAsync(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] ExecuteEx(string jobID, ScriptExecution script)
        {
            return ExecuteEx(jobID, script, 5, "localhost");
        }

        public LuaValue[] ExecuteEx(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.ExecuteEx(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.ExecuteEx(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] ExecuteEx(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.ExecuteEx(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.ExecuteEx(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script)
        {
            return await ExecuteExAsync(jobID, script, 5, "localhost");
        }

        public async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.ExecuteExAsync(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.ExecuteExAsync(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> ExecuteExAsync(string name, string jobID, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.ExecuteExAsync(jobID, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.ExecuteExAsync(jobID, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public void CloseJob(string jobID)
        {
            CloseJob(jobID, 5, "localhost");
        }

        public void CloseJob(string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) { SoapUtility.Singleton.CloseJob(jobID); return; }

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                instance.CloseJob(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public void CloseJob(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) { SoapUtility.Singleton.CloseJob(jobID); return; }

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                instance.CloseJob(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task CloseJobAsync(string jobID)
        {
            await CloseJobAsync(jobID, 5, "localhost");
        }

        public async Task CloseJobAsync(string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) { await SoapUtility.Singleton.CloseJobAsync(jobID); return; }

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                await instance.CloseJobAsync(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task CloseJobAsync(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) { await SoapUtility.Singleton.CloseJobAsync(jobID); return; }

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                await instance.CloseJobAsync(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] BatchJob(Job job, ScriptExecution script)
        {
            return BatchJob(job, script, 5, "localhost");
        }

        public LuaValue[] BatchJob(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.BatchJob(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.BatchJob(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] BatchJob(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.BatchJob(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.BatchJob(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script)
        {
            return await BatchJobAsync(job, script, 5, "localhost");
        }

        public async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.BatchJobAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.BatchJobAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<BatchJobResponse> BatchJobAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.BatchJobAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.BatchJobAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] BatchJobEx(Job job, ScriptExecution script)
        {
            return BatchJobEx(job, script, 5, "localhost");
        }

        public LuaValue[] BatchJobEx(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.BatchJobEx(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.BatchJobEx(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] BatchJobEx(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.BatchJobEx(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.BatchJobEx(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script)
        {
            return await BatchJobExAsync(job, script, 5, "localhost");
        }

        public async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.BatchJobExAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.BatchJobExAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> BatchJobExAsync(string name, Job job, ScriptExecution script, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.BatchJobExAsync(job, script);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.BatchJobExAsync(job, script);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public double GetExpiration(string jobID)
        {
            return GetExpiration(jobID, 5, "localhost");
        }

        public double GetExpiration(string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetExpiration(jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.GetExpiration(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public double GetExpiration(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetExpiration(jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.GetExpiration(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<double> GetExpirationAsync(string jobID)
        {
            return await GetExpirationAsync(jobID, 5, "localhost");
        }

        public async Task<double> GetExpirationAsync(string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetExpirationAsync(jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.GetExpirationAsync(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<double> GetExpirationAsync(string name, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetExpirationAsync(jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.GetExpirationAsync(jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public Job[] GetAllJobs()
        {
            return GetAllJobs(5, "localhost");
        }

        public Job[] GetAllJobs(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetAllJobs();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.GetAllJobs();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public Job[] GetAllJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetAllJobs();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.GetAllJobs();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<GetAllJobsResponse> GetAllJobsAsync()
        {
            return await GetAllJobsAsync(5, "localhost");
        }

        public async Task<GetAllJobsResponse> GetAllJobsAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetAllJobsAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.GetAllJobsAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<GetAllJobsResponse> GetAllJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetAllJobsAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.GetAllJobsAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public Job[] GetAllJobsEx()
        {
            return GetAllJobsEx(5, "localhost");
        }

        public Job[] GetAllJobsEx(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetAllJobsEx();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.GetAllJobsEx();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public Job[] GetAllJobsEx(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.GetAllJobsEx();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.GetAllJobsEx();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<Job[]> GetAllJobsExAsync()
        {
            return await GetAllJobsExAsync(5, "localhost");
        }

        public async Task<Job[]> GetAllJobsExAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetAllJobsExAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.GetAllJobsExAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<Job[]> GetAllJobsExAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.GetAllJobsExAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.GetAllJobsExAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public int CloseExpiredJobs()
        {
            return CloseExpiredJobs(5, "localhost");
        }

        public int CloseExpiredJobs(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.CloseExpiredJobs();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.CloseExpiredJobs();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public int CloseExpiredJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.CloseExpiredJobs();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.CloseExpiredJobs();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<int> CloseExpiredJobsAsync()
        {
            return await CloseExpiredJobsAsync(5, "localhost");
        }

        public async Task<int> CloseExpiredJobsAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.CloseExpiredJobsAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.CloseExpiredJobsAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<int> CloseExpiredJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.CloseExpiredJobsAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.CloseExpiredJobsAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public int CloseAllJobs()
        {
            return CloseAllJobs(5, "localhost");
        }

        public int CloseAllJobs(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.CloseAllJobs();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.CloseAllJobs();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public int CloseAllJobs(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.CloseAllJobs();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.CloseAllJobs();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<int> CloseAllJobsAsync()
        {
            return await CloseAllJobsAsync(5, "localhost");
        }

        public async Task<int> CloseAllJobsAsync(int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.CloseAllJobsAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.CloseAllJobsAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<int> CloseAllJobsAsync(string name, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.CloseAllJobsAsync();

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.CloseAllJobsAsync();
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] Diag(int type, string jobID)
        {
            return Diag(type, jobID, 5, "localhost");
        }

        public LuaValue[] Diag(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.Diag(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.Diag(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] Diag(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.Diag(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.Diag(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<DiagResponse> DiagAsync(int type, string jobID)
        {
            return await DiagAsync(type, jobID, 5, "localhost");
        }

        public async Task<DiagResponse> DiagAsync(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.DiagAsync(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.DiagAsync(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<DiagResponse> DiagAsync(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.DiagAsync(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.DiagAsync(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] DiagEx(int type, string jobID)
        {
            return DiagEx(type, jobID, 5, "localhost");
        }

        public LuaValue[] DiagEx(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.DiagEx(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return instance.DiagEx(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public LuaValue[] DiagEx(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return SoapUtility.Singleton.DiagEx(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return instance.DiagEx(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> DiagExAsync(int type, string jobID)
        {
            return await DiagExAsync(type, jobID, 5, "localhost");
        }

        public async Task<LuaValue[]> DiagExAsync(int type, string jobID, int maxAttemptsToHitGridServer, string hostName)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.DiagExAsync(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreateAvailableInstance(maxAttemptsToHitGridServer, hostName);

            try
            {
                return await instance.DiagExAsync(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        public async Task<LuaValue[]> DiagExAsync(string name, int type, string jobID, int maxAttemptsToHitGridServer = 5, string hostName = "localhost", bool isPoolable = false)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer) return await SoapUtility.Singleton.DiagExAsync(type, jobID);

            SystemUtility.Singleton.OpenWebServerIfNotOpen();

            var instance = GetOrCreatePersistentInstance(name, maxAttemptsToHitGridServer, hostName, isPoolable);

            try
            {
                return await instance.DiagExAsync(type, jobID);
            }
            catch (Exception ex) { throw ex; }
            finally
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
        }

        // should this be an IDisposable?
        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        public sealed class GridServerInstance : ComputeCloudServiceSoapClient, IDisposable
        {
            private readonly int _maxAttemptsToHitGridServer;
            private readonly bool _isPersistent;
            private int _gridServerProcessID;
            private TimeSpan _totalTimeToOpenInstance;
            private readonly string _name;
            private bool _isAvailable;
            private readonly bool _isPoolable;
            private readonly object _availableLock = new object();

            public bool IsOpened { get { try { return _gridServerProcessID != 0; } catch { return true; } } }
            public int ProcessID { get { return _gridServerProcessID; } }
            public bool Persistent { get { return _isPersistent; } }
            public int MaxAttemptsToGetResultFromGridServer { get { return _maxAttemptsToHitGridServer; } }
            public string Name { get { return _name; } }
            /// If this is TimeSpan.Zero, it means it was already open
            /// elias: well it would have if we bothered to make the port check faster :)
            public TimeSpan TotalTimeToOpenInstance { get { return _totalTimeToOpenInstance; } }
            public bool IsAvailable { get { return _isAvailable; } }
            public bool IsPoolable { get { return _isPoolable; } }
            public int Port { get { return Endpoint.Address.Uri.Port; } }

            private static readonly Binding DefaultHTTPBinding =
                new BasicHttpBinding(BasicHttpSecurityMode.None)
                {
                    MaxReceivedMessageSize = int.MaxValue,
                    SendTimeout = global::MFDLabs.Grid.Bot.Properties.Settings.Default.SoapUtilityRemoteServiceTimeout
                };



            public GridServerInstance(string host, int port, string name, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : this(new EndpointAddress($"http://{host}:{port}"), name, maxAttemptsToHitGridServer, persistent, poolable, openNowInNewThread)
            { }

            public GridServerInstance(EndpointAddress remoteAddress, string name, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : this(remoteAddress, name, false, maxAttemptsToHitGridServer, persistent, poolable, openNowInNewThread)
            {
            }

            public GridServerInstance(string host, int port, string name, bool openProcessNow, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
                : this(new EndpointAddress($"http://{host}:{port}"), name, openProcessNow, maxAttemptsToHitGridServer, persistent, poolable, openNowInNewThread)
            { }

            public GridServerInstance(EndpointAddress remoteAddress, string name, bool openProcessNow, int maxAttemptsToHitGridServer = 5, bool persistent = false, bool poolable = true, bool openNowInNewThread = false)
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

            public new string HelloWorld()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.HelloWorld();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }


            public new async Task<string> HelloWorldAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.HelloWorldAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new string GetVersion()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.GetVersion();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<string> GetVersionAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.GetVersionAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new Status GetStatus()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.GetStatus();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<Status> GetStatusAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.GetStatusAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] OpenJob(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.OpenJob(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.OpenJobAsync(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] OpenJobEx(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.OpenJobEx(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.OpenJobExAsync(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new double RenewLease(string jobID, double expirationInSeconds)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.RenewLease(jobID, expirationInSeconds);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<double> RenewLeaseAsync(string jobID, double expirationInSeconds)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.RenewLeaseAsync(jobID, expirationInSeconds);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] Execute(string jobID, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.Execute(jobID, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.ExecuteAsync(jobID, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] ExecuteEx(string jobID, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.ExecuteEx(jobID, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.ExecuteExAsync(jobID, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new void CloseJob(string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            base.CloseJob(jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task CloseJobAsync(string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            await base.CloseJobAsync(jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] BatchJob(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.BatchJob(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.BatchJobAsync(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] BatchJobEx(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.BatchJobEx(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.BatchJobExAsync(job, script);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new double GetExpiration(string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.GetExpiration(jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<double> GetExpirationAsync(string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.GetExpirationAsync(jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new Job[] GetAllJobs()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.GetAllJobs();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<GetAllJobsResponse> GetAllJobsAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.GetAllJobsAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new Job[] GetAllJobsEx()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.GetAllJobsEx();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<Job[]> GetAllJobsExAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.GetAllJobsExAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new int CloseExpiredJobs()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.CloseExpiredJobs();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<int> CloseExpiredJobsAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.CloseExpiredJobsAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new int CloseAllJobs()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.CloseAllJobs();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<int> CloseAllJobsAsync()
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.CloseAllJobsAsync();
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] Diag(int type, string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.Diag(type, jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<DiagResponse> DiagAsync(int type, string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.DiagAsync(type, jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new LuaValue[] DiagEx(int type, string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return base.DiagEx(type, jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            public new async Task<LuaValue[]> DiagExAsync(int type, string jobID)
            {
                try
                {
                    lock (_availableLock)
                        _isAvailable = false;
                    if (!IsOpened)
                    {
                        if (!TryOpen()) throw new ApplicationException("Unable to open grid server instance.");
                    }

                    var currentMethod = MethodBase.GetCurrentMethod().Name;

                    for (int i = 0; i < _maxAttemptsToHitGridServer; i++)
                    {
                        try
                        {
                            return await base.DiagExAsync(type, jobID);
                        }
                        catch (EndpointNotFoundException)
                        {
                            SystemLogger.Singleton.Warning("The grid server instance command the name of '{0}->{1}' threw an EndpointNotFoundException, opening and retrying...", _name, currentMethod);
                            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                                if (!TryOpen()) throw new ApplicationException($"Unable to open grid server instance '{_name}'.");
                        }
                        catch (FaultException ex) { throw ex; }
                        catch (TimeoutException ex) { throw ex; }
                        catch (Exception ex)
                        {
                            SystemLogger.Singleton.Warning("Exception occurred when trying to execute command '{0}->{1}': {2}. Retrying...", _name, currentMethod, ex.Message); // #if DEBUG here to show the full exception?
                                                                                                                                                                                // back off here?
                        }
                    }

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts)
                        throw new TimeoutException($"The command '{_name}->{currentMethod}' reached it's max attempts to give a result.");

                    return default;
                }
                finally
                {
                    lock (_availableLock)
                        _isAvailable = true;
                }
            }

            #region Auto-Generated Items

            private string GetDebuggerDisplay() => _name;

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
