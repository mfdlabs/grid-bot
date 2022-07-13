using System;
using System.Collections.Generic;
using MFDLabs.Hashicorp.ConsulClient;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class ConsulServiceRegistrationUtility
    {
        private static readonly IConsulClient _consulClient;

        static ConsulServiceRegistrationUtility()
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationEnabled)
            {
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationUrl.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationUrl));

                var configuration = new ConsulClientConfiguration();
                configuration.Address = new Uri(global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationUrl);

                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationAclToken.IsNullOrEmpty())
                    configuration.Token = global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationAclToken;

                _consulClient = new ConsulClient(configuration);
            }
        }

        private static void CheckClientPresent()
        {
            if (_consulClient == null)
                throw new ApplicationException("The Consul Client for ServiceRegistration was null, ConsulServiceRegistrationEnabled may be False");
        }

        public static string RegisterSubService(string baseServiceName, string name, bool enableBasicHealthChecks = true, string address = null, int? port = null, string[] tags = null)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationEnabled)
            {

                if (baseServiceName == null)
                    throw new ArgumentNullException(nameof(name));
                if (name == null)
                    name = $"{baseServiceName}-{NetworkingGlobal.GenerateUuidv4()}";

                CheckClientPresent();

                if (address == null)
                    address = NetworkingGlobal.GetLocalIp();

                var registration = new AgentServiceRegistration()
                {
                    ID = name,
                    Name = baseServiceName,
                    Address = address,
                    Port = port ?? 0,
                    Tags = tags ?? Array.Empty<string>()
                };

                if (enableBasicHealthChecks)
                {
                    registration.Check = new AgentServiceCheck
                    {
                        Name = $"{name} Health Checks",
                        Status = HealthStatus.Critical,
                        Notes = $"{name} Health Checks",
                        TTL = TimeSpan.FromSeconds(1) // Set it to MaxValue for now until we setup some sort of thread to check health and report it.
                    };
                }

                _consulClient.Agent.ServiceRegister(registration).Wait();
            }

            return name;
        }

        public static void DeregisterService(string name)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationEnabled)
            {

                if (name == null) throw new ArgumentNullException(nameof(name));

                CheckClientPresent();

                _consulClient.Agent.ServiceDeregister(name).Wait();
            }
        }

        public static void RegisterServiceHttpCheck(
            string serviceName,
            string name,
            string url,
            string notes = null,
            TimeSpan? interval = null,
            string method = "GET",
            string body = null,
            Dictionary<string, List<string>> header = null
        )
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationEnabled)
            {

                if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (url == null) throw new ArgumentNullException(nameof(url));

                CheckClientPresent();

                if (interval == null) interval = TimeSpan.FromSeconds(1);
                if (header == null) header = new();

                var check = new AgentCheckRegistration
                {
                    Name = name,
                    ServiceID = serviceName,
                    HTTP = url,
                    Interval = interval,
                    Method = method,
                    Body = body,
                    Header = header,
                    Notes = notes
                };

                _consulClient.Agent.CheckRegister(check).Wait();
            }
        }

        public static void RegisterService(string name, bool enableBasicHealthChecks = true, string address = null, int? port = null, string[] tags = null)
        {
            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ConsulServiceRegistrationEnabled)
            {

                if (name == null)
                    throw new ArgumentNullException(nameof(name));

                CheckClientPresent();

                if (address == null)
                    address = NetworkingGlobal.GetLocalIp();

                var registration = new AgentServiceRegistration()
                {
                    Name = name,
                    Address = address,
                    Port = port ?? 0,
                    Tags = tags ?? Array.Empty<string>()
                };

                if (enableBasicHealthChecks)
                {
                    registration.Check = new AgentServiceCheck
                    {
                        Name = $"{name} Health Checks",
                        Status = HealthStatus.Critical,
                        Notes = $"{name} Health Checks",
                        TTL = TimeSpan.FromSeconds(1) // Set it to MaxValue for now until we setup some sort of thread to check health and report it.
                    };
                }

                _consulClient.Agent.ServiceRegister(registration).Wait();
            }
        }
    }
}
