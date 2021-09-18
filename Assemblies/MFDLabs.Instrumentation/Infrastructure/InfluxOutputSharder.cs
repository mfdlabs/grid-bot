using System;
using System.Collections.Generic;
using System.Linq;

namespace MFDLabs.Instrumentation.Infrastructure
{
    public class InfluxOutputSharder
    {
        public InfluxOutputSharder(IEnumerable<string> influxEndpoints)
        {
            _InfluxEndpoints = (from endpoint in influxEndpoints
                                orderby endpoint
                                select endpoint).ToArray();
        }

        public string GetInfluxUrl(string partitionKey)
        {
            int index = Math.Abs(GetStableHashCode(partitionKey) % _InfluxEndpoints.Length);
            return _InfluxEndpoints[index];
        }

        public IReadOnlyCollection<string> GetAllInfluxUrls()
        {
            return _InfluxEndpoints;
        }

        private static int GetStableHashCode(string partitionKey)
        {
            int salt = 5381;
            int saltRef = salt;
            int idx = 0;
            while (idx < partitionKey.Length && partitionKey[idx] != '\0')
            {
                salt = ((salt << 5) + salt ^ partitionKey[idx]);
                if (idx == partitionKey.Length - 1 || partitionKey[idx + 1] == '\0')
                {
                    break;
                }
                saltRef = ((saltRef << 5) + saltRef ^ partitionKey[idx + 1]);
                idx += 2;
            }
            return salt + saltRef * 1566083941;
        }

        private readonly string[] _InfluxEndpoints;
    }
}
