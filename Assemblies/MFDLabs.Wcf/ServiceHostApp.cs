using System;
using System.ServiceModel;
using System.ServiceProcess;
using System.Configuration.Install;
using MFDLabs.Logging;
using System.Reflection;
using System.Collections;

namespace MFDLabs.Wcf
{
    public interface IArgumentService
    {
        void ProcessArgs(string[] args);
    }
    public class ServiceBasePublic : ServiceBase
    {
        public void OnStartPublic(string[] args)
        {
            OnStart(args);
        }

        public void OnStopPublic()
        {
            OnStop();
        }
    }

    public class ServiceHostApp : ServiceHostApp<dynamic> {}

    /// <summary>
    /// An NT Service that hosts a WCF Service.
    /// This class encapsulates a ServiceHost.
    /// </summary>
    public class ServiceHostApp<TServiceClass> : ServiceBasePublic
        where TServiceClass : class
    {
        // Events forwarded from the encapsulated ServiceHost
        public event EventHandler HostClosed;
        public event EventHandler HostClosing;
        public event EventHandler HostFaulted;
        public event EventHandler HostOpened;
        public event EventHandler HostOpening;

        ServiceHost serviceHost = null;
        readonly TServiceClass singleton;


        readonly ServiceBasePublic[] otherSingletons = null;

        public ServiceHostApp(TServiceClass singleton)
        {
            this.singleton = singleton;
        }
        public ServiceHostApp(TServiceClass singleton, ServiceBasePublic[] others)
            : this(singleton)
        {
            otherSingletons = others;
        }

        public ServiceHostApp()
        {
        }

        protected override void OnStart(string[] args)
        {
            CloseServiceHost();

            if (singleton != null)
                serviceHost = new ServiceHost(singleton);
            else
                serviceHost = new ServiceHost(typeof(TServiceClass));

            if (serviceHost.SingletonInstance is IArgumentService argumentService)
                argumentService.ProcessArgs(args);

            serviceHost.Closed += ServiceHost_Closed;
            serviceHost.Closing += ServiceHost_Closing;
            serviceHost.Faulted += ServiceHost_Faulted;
            serviceHost.Opened += ServiceHost_Opened;
            serviceHost.Opening += ServiceHost_Opening;

            serviceHost.Open();

            if (otherSingletons != null)
                foreach (var sbp in otherSingletons)
                    sbp.OnStartPublic(args);
        }

        private void CloseServiceHost()
        {
            if (serviceHost != null)
            {
                if (serviceHost.State != CommunicationState.Closed)
                    serviceHost.Close();
                serviceHost.Closed -= ServiceHost_Closed;
                serviceHost.Closing -= ServiceHost_Closing;
                serviceHost.Faulted -= ServiceHost_Faulted;
                serviceHost.Opened -= ServiceHost_Opened;
                serviceHost.Opening -= ServiceHost_Opening;
                serviceHost = null;
            }
        }

        void ServiceHost_Closed(object sender, EventArgs e) => HostClosed?.Invoke(sender, e);
        void ServiceHost_Closing(object sender, EventArgs e) => HostClosing?.Invoke(sender, e);
        void ServiceHost_Faulted(object sender, EventArgs e) => HostFaulted?.Invoke(sender, e);
        void ServiceHost_Opened(object sender, EventArgs e) => HostOpened?.Invoke(sender, e);
        void ServiceHost_Opening(object sender, EventArgs e) => HostOpening?.Invoke(sender, e);

        protected override void OnStop()
        {
            CloseServiceHost();

            if (otherSingletons != null)
                foreach (var sbp in otherSingletons)
                    sbp.OnStopPublic();
        }

        public void Process(string[] args) => Process(args, null);
        /// <summary>
        /// Process arguments
        /// </summary>
        /// <param name="args"></param>
        /// <param name="statsTask">A task to perform when the user presses a key</param>
        public void Process(string[] args, Action statsTask)
        {
            if (EventLog != null)
                EventLogLogger.Singleton.SetEventLog(EventLog);

            if (args.Length > 0)
            {
                try
                {
                    string option = args[0].StartsWith("--") // Starts with 2, split 2
                        ? args[0].Substring(2).ToLower()
                        : args[0].Substring(1).ToLower();
                    if (option == "console")
                    {
                        var type = typeof(TServiceClass);
                        EventLogLogger.Singleton.LifecycleEvent("Starting {0}...", type == typeof(object) ? "ServiceHostApp" : type.Name);
                        OnStart(args);

                        EventLogLogger.Singleton.Warning("Service started. Press any key to {0}.", statsTask == null ? "exit" : "get stats");
                        Logger.Singleton.Log("Press {0} to force a full Garbage Collection cycle", ConsoleKey.G);
                        Logger.Singleton.Log("Press {0} to close sockets or {1} to exit process", ConsoleKey.Q, ConsoleKey.Escape);

                        while (true)
                        {
                            ConsoleKey key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.Escape)
                                break;
                            else if (key == ConsoleKey.G)
                            {
                                EventLogLogger.Singleton.LifecycleEvent("Initiating GC cycle...");
                                GC.Collect(3, GCCollectionMode.Forced);
                                EventLogLogger.Singleton.Log("Finished GC Cycle!");
                            }
                            else if (key == ConsoleKey.Q)
                            {
                                //Close Sockets but keep the process alive
                                EventLogLogger.Singleton.LifecycleEvent("Closing sockets...");
                                CloseServiceHost();
                                EventLogLogger.Singleton.Log("Sucessfully closed all sockets!");
                                EventLogLogger.Singleton.Log("Press {0} to exit process", ConsoleKey.Escape);
                            }
                            else
                            {
                                if (statsTask != null)
                                    statsTask();
                                else
                                    break;
                            }
                        }
                        OnStop();
                    }
                    else if (option == "install")
                    {
                        // NOTE: This only works if the assembly has an appropriate Installer object in it
                        var installer = new AssemblyInstaller(Assembly.GetEntryAssembly(), Array.Empty<string>())
                        {
                            UseNewContext = true
                        };
                        var savedState = new Hashtable();

                        installer.Install(savedState);
                        installer.Commit(savedState);
                        EventLogLogger.Singleton.Info("Service Installed!");
                    }
                    else if (option == "uninstall")
                    {
                        // NOTE: This only works if the assembly has an appropriate Installer object in it
                        var assemblyInstaller = new AssemblyInstaller(Assembly.GetEntryAssembly(), Array.Empty<string>())
                        {
                            UseNewContext = true
                        };
                        var newState = new Hashtable();
                        assemblyInstaller.Uninstall(newState);
                    }

                    // Just skip unknown args
                }
                catch (Exception e)
                {
                    EventLogLogger.Singleton.Error(e);
                }
            }
            else
            {
                if (otherSingletons != null)
                {
                    var hosts = new ServiceBase[otherSingletons.Length + 1];
                    int pos = 0;
                    hosts[pos++] = this;
                    foreach (var sb in otherSingletons)
                        hosts[pos++] = sb;
                    Run(hosts);
                }
                else Run(this);
            }
        }
    }
}