using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using Text.Extensions;

namespace Instrumentation.Infrastructure
{
    internal class InfrastructureServiceConfigurationProvider : IDisposable, IConfigurationProvider
    {
        public InfrastructureServiceConfigurationProvider(string machineName, Action<Exception> exceptionHandler)
        {
            if (!machineName.IsNullOrEmpty())
                _MachineName = machineName;
            else
            {
                var machineNameVariable = Environment.GetEnvironmentVariable(_MachineNameVariableKey, EnvironmentVariableTarget.Process);
                machineNameVariable = machineNameVariable ?? Environment.GetEnvironmentVariable(_MachineNameVariableKey, EnvironmentVariableTarget.User);
                machineNameVariable = machineNameVariable ?? Environment.GetEnvironmentVariable(_MachineNameVariableKey, EnvironmentVariableTarget.Machine);
                _MachineName = machineNameVariable ?? Environment.MachineName.ToUpperInvariant();
            }

            _ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException("exceptionHandler");
            _ConfigurationUrl = string.Format("https://{0}/v2/GetPerfmonConfiguration?hostName={1}", _InfrastructureServiceEndpoint, Uri.EscapeDataString(_MachineName));
            _Timer = new Timer(s => ReloadConfiguration(), null, _ConfigurationReloadInterval, _ConfigurationReloadInterval);
        }

        public ICollectionConfiguration GetConfiguration()
        {
            if (_CollectionConfiguration == null) ReloadConfiguration();
            return _CollectionConfiguration;
        }
        public void Dispose()
        {
            if (_Timer == null) return;
            _Timer.Dispose();
        }
        private void ReloadConfiguration()
        {
            try
            {
                _Timer.Change(-1, -1);
                byte[] response;
                using (var webClient = new WebClient()) 
                    response = webClient.DownloadData(_ConfigurationUrl);
                if (_LastJsonConfiguration == null || !_LastJsonConfiguration.SequenceEqual(response) || _CollectionConfiguration == null)
                {
                    using (var memoryStream = new MemoryStream(response))
                    {
                        var configurationDto = (ConfigurationDto)new DataContractJsonSerializer(typeof(ConfigurationDto)).ReadObject(memoryStream);
                        _CollectionConfiguration = new InfrastructureServiceCollectionConfiguration(
                            _MachineName,
                            configurationDto.ServerFarmName,
                            configurationDto.SuperFarmName,
                            configurationDto.PerfmonDatabase,
                            configurationDto.InfluxDbUrls,
                            null,
                            configurationDto.IsInfluxDbShardingOnWritesEnabled
                        );
                    }
                    _LastJsonConfiguration = response;
                }
            }
            catch (Exception ex) { try { _ExceptionHandler?.Invoke(ex); } catch { } }
            finally { _Timer.Change(_ConfigurationReloadInterval, _ConfigurationReloadInterval); }
        }

        private const string _InfrastructureServiceEndpoint = "infrastructure.simulping.com";
        private const string _MachineNameVariableKey = "InstrumentationMachineName";
        private static readonly TimeSpan _ConfigurationReloadInterval = TimeSpan.FromMinutes(10.0);
        private readonly string _MachineName;
        private readonly Action<Exception> _ExceptionHandler;
        private readonly string _ConfigurationUrl;
        private readonly Timer _Timer;
        private ICollectionConfiguration _CollectionConfiguration;
        private byte[] _LastJsonConfiguration;

        [DataContract]
        public class ConfigurationDto
        {
            [DataMember]
            public string ServerFarmName { get; set; }
            [DataMember]
            public string SuperFarmName { get; set; }
            [DataMember]
            public string PerfmonDatabase { get; set; }
            [DataMember]
            public string[] InfluxDbUrls { get; set; }
            [DataMember]
            public bool IsInfluxDbShardingOnWritesEnabled { get; set; }
        }
    }
}
