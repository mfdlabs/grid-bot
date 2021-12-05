using System.Collections.Generic;

namespace MFDLabs.Instrumentation
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public ConfigurationProvider(string hostIdentifier, string influxDatabaseName, IReadOnlyList<string> influxEndpoints, string influxUsername, string influxPassword)
            : this(
                  hostIdentifier,
                  null,
                  null,
                  influxDatabaseName,
                  influxEndpoints,
                  new InfluxCredentials
                  {
                      Username = influxUsername,
                      Password = influxPassword
                  }
             )
        { }
        public ConfigurationProvider(string hostIdentifier, string influxDatabaseName, IReadOnlyList<string> influxEndpoints)
            : this(hostIdentifier, null, null, influxDatabaseName, influxEndpoints, null)
        { }
        public ConfigurationProvider(string hostIdentifier, string farmIdentifier, string superFarmIdentifier, string influxDatabaseName, IReadOnlyList<string> influxEndpoints, InfluxCredentials influxCredentials = null) 
            => _Configuration = new CollectionConfiguration(hostIdentifier, farmIdentifier, superFarmIdentifier, influxDatabaseName, influxEndpoints, influxCredentials);

        public ICollectionConfiguration GetConfiguration() => _Configuration;

        private readonly ICollectionConfiguration _Configuration;
    }
}
