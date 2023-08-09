using System;

using Consul;

using Text.Extensions;
using Threading.Extensions;
using Configuration.Logging;

namespace Configuration
{
    internal static class ConsulServiceDiscovery
    {
        private static readonly IConsulClient _consulClient;

        static ConsulServiceDiscovery()
        {
            if (global::Configuration.Properties.Settings.Default.ConsulServiceDiscoveryEnabled)
            {
                if (global::Configuration.Properties.Settings.Default.ConsulServiceDiscoveryUrl.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(global::Configuration.Properties.Settings.Default.ConsulServiceDiscoveryUrl));

                var configuration = new ConsulClientConfiguration();
                configuration.Address = new Uri(global::Configuration.Properties.Settings.Default.ConsulServiceDiscoveryUrl);

                if (!global::Configuration.Properties.Settings.Default.ConsulServiceDiscoveryAclToken.IsNullOrEmpty())
                    configuration.Token = global::Configuration.Properties.Settings.Default.ConsulServiceDiscoveryAclToken;

                _consulClient = new ConsulClient(configuration);
            }
        }

        private static void CheckClientPresent()
        {
            if (_consulClient == null)
                throw new ApplicationException("The Consul Client for ServiceDiscovery was null, ConsulServiceDiscoveryEnabled may be False");
        }

        public static CatalogService GetService(string serviceName)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));

            CheckClientPresent();

            try
            {
                var service = _consulClient.Catalog.Service(serviceName).Sync().Response;

                if (service.Length > 0)
                {
                    return service[0];
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static int? GetServicePort(string serviceName)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));

            CheckClientPresent();

            var service = GetService(serviceName);

            var port = service?.ServicePort;

            if (port.HasValue)
                ConfigurationLogging.Info($"Sucessfully discovered consul service '{serviceName}' port {port.Value}");
            else
                ConfigurationLogging.Warning($"Unable to discover consul service '{serviceName}' port, because it did not exist or had no Port attribute.");

            return port;
        }

        public static string GetFullyQualifiedServiceURL(string serviceName)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));

            CheckClientPresent();

            var host = GetServiceAddress(serviceName);
            if (host == null) return null;

            var port = GetServicePort(serviceName);
            if (!port.HasValue) return null;

            var url = $"http://{host}:{port}";

            if (host != null && port.HasValue)
                ConfigurationLogging.Info($"Sucessfully discovered consul service '{serviceName}' FQSUrl {url}");
            else
                ConfigurationLogging.Warning($"Unable to discover consul service '{serviceName}' FQSUrl, because it did not exist or didn't have sufficient attributes.");

            return url;
        }

        public static string GetServiceAddress(string serviceName)
        {
            if (serviceName == null) throw new ArgumentNullException(nameof(serviceName));

            CheckClientPresent();

            var service = GetService(serviceName);

            var addr = service?.ServiceAddress ?? service?.Address;

            if (addr != null)
                ConfigurationLogging.Info($"Sucessfully discovered consul service '{serviceName}' address {addr}");
            else
                ConfigurationLogging.Warning($"Unable to discover consul service '{serviceName}' addr, because it did not exist or had no ServiceAddress or Address attribute.");

            return addr;
        }

        public static bool ServiceExists(string serviceName) => GetServiceAddress(serviceName) != null;

    }
}
