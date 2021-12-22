using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using MFDLabs.Configuration.Logging;
using MFDLabs.Configuration.Settings;
using MFDLabs.Hashicorp.ConsulClient;

namespace MFDLabs.Configuration.Clients.ConsulKv
{
    public class ConsulKvConfigurationClient : IDisposable
    {
        public ConsulKvConfigurationClient(string address, string token) 
            => _client = new ConsulClient(new ConsulClientConfiguration { Address = new Uri(address), Token = token });

        public static IReadOnlyCollection<T> FetchWithRetries<T>(Func<string, IReadOnlyCollection<T>> getterFunc, string groupName, int maxAttempts)
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    return getterFunc(groupName);
                }
                catch (Exception ex)
                {
                    ConfigurationLogging.Error(ex.ToString());
                    Thread.Sleep(i * global::MFDLabs.Configuration.Properties.Settings.Default.ConsulKVConfigurationFetcherBackoffBaseMilliseconds);
                }
            }
            return Array.Empty<T>();
        }

        public IReadOnlyCollection<ISetting> GetAllSettings(string groupName)
        {
            var keys = _client.KV.Keys($"mfdlabs-sharp/{groupName}/Settings").Result;
            var settings = new List<Setting>();

            foreach (var key in keys.Response)
            {
                var buffer = _client.KV.Get(key).Result.Response.Value;
                object data;
                using (var stream = new MemoryStream(buffer))
                {
                    var formatter = new BinaryFormatter();
                    data = formatter.Deserialize(stream);
                }

                if (!(data is Setting setting)) continue; // TODO: Something here?

                settings.Add(setting);
            }
            return settings;
        }

        public IReadOnlyCollection<IConnectionString> GetAllConnectionStrings(string groupName)
        {
            var keys = _client.KV.Keys($"mfdlabs-sharp/{groupName}/ConnectionStrings").Result;
            var connectionStrings = new List<ConnectionString>();

            foreach (var key in keys.Response)
            {
                var buffer = _client.KV.Get(key).Result.Response.Value;
                object data;
                using (var stream = new MemoryStream(buffer))
                {
                    var formatter = new BinaryFormatter();
                    data = formatter.Deserialize(stream);
                }

                if (!(data is ConnectionString connectionString)) continue; // TODO: Something here?

                connectionStrings.Add(connectionString);
            }
            return connectionStrings;
        }

        public void SetProperty(string groupName, string name, string type, string value, DateTime updated)
        {
            var isConnectionString = name.ToLower().Contains("connectionstring") && type == typeof(string).FullName;
            var prefix = isConnectionString ? $"mfdlabs-sharp/{groupName}/ConnectionStrings" : $"mfdlabs-sharp/{groupName}/Settings";
            byte[] data = null;
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                if (isConnectionString)
                    formatter.Serialize(stream, new ConnectionString() { GroupName = groupName, Name = name, Value = value, Updated = updated });
                else
                    formatter.Serialize(stream, new Setting() { GroupName = groupName, Name = name, Type = type, Value = value, Updated = updated });
                data = stream.ToArray();
            }
            _client.KV.Put(new KVPair(prefix) { Value = data }).Wait();
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            _client?.Dispose();
        }

        private readonly IConsulClient _client;
    }
}
