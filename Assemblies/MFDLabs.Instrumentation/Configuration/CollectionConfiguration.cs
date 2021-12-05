using System.Collections.Generic;

namespace MFDLabs.Instrumentation
{
    internal class CollectionConfiguration : ICollectionConfiguration
    {
        public string HostIdentifier { get; }
        public string FarmIdentifier { get; }
        public string SuperFarmIdentifier { get; }
        public string InfluxDatabaseName { get; }
        public InfluxCredentials InfluxCredentials { get; }

        public CollectionConfiguration(string hostIdentifier, string farmIdentifier, string superFarmIdentifier, string influxDatabaseName, IReadOnlyList<string> influxEndpoints, InfluxCredentials influxCredentials)
        {
            HostIdentifier = hostIdentifier;
            FarmIdentifier = farmIdentifier;
            SuperFarmIdentifier = superFarmIdentifier;
            InfluxDatabaseName = influxDatabaseName;
            _InfluxEndpoints = influxEndpoints;
            InfluxCredentials = influxCredentials;
        }

        public IReadOnlyCollection<string> GetInfluxEndpointsForCategory(string category) => _InfluxEndpoints;

        private readonly IReadOnlyList<string> _InfluxEndpoints;
    }
}
