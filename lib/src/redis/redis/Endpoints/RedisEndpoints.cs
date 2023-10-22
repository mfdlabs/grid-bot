namespace Redis;

using System;
using System.Linq;
using System.Configuration;
using System.ComponentModel;
using System.Collections.Generic;

/// <summary>
/// Represents a configuration class for Redis Endpoints.
/// </summary>
[TypeConverter(typeof(RedisEndpointsConverter))]
[SettingsSerializeAs(SettingsSerializeAs.String)]
public class RedisEndpoints
{
    /// <summary>
    /// Get the raw Redis endponts.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Get the endpoints parsed from the string.
    /// </summary>
    public ICollection<string> Endpoints { get; }

    /// <summary>
    /// Construct a new instance of <see cref="RedisEndpoints"/>
    /// </summary>
    /// <param name="hostsWithPorts">The hosts with their ports.</param>
    public RedisEndpoints(string hostsWithPorts)
    {
        Source = hostsWithPorts;
        Endpoints = ParseConfiguration(hostsWithPorts);
    }

    private List<string> ParseConfiguration(string hostsWithPorts)
    {
        return (from str in hostsWithPorts.Split(',').SelectMany(ParseEntry)
                orderby str
                select str).ToList();
    }

    private List<string> ParseEntry(string hostWithPorts)
    {
        var split = hostWithPorts.Split(':');

        if (split.Length != 2)
            throw new RedisEndpointParseException(string.Format("Entry did not contain and host and port/port-range pair: \"{0}\"", hostWithPorts));

        var host = split[0];
        var portOrPortRange = split[1].Split('-');

        if (portOrPortRange.Length == 1) return new List<string> { hostWithPorts };

        if (portOrPortRange.Length == 2)
        {
            try
            {
                int start = int.Parse(portOrPortRange[0]);
                int end = int.Parse(portOrPortRange[1]);

                if (end < start)
                    throw new RedisEndpointParseException(string.Format("Entry's end port is lower than the start port: \"{0}\"", hostWithPorts));

                var endpoints = new List<string>();

                for (int i = start; i <= end; i++)
                    endpoints.Add(string.Format("{0}:{1}", host, i));

                return endpoints;
            }
            catch (FormatException)
            {
                throw new RedisEndpointParseException(string.Format("Entry has unparseable start and end port numbers: \"{0}\"", hostWithPorts));
            }
        }

        throw new RedisEndpointParseException(string.Format("Entry has unparseable port range: \"{0}\"", hostWithPorts));
    }

    /// <summary>
    /// Does the specified endpoints have the same endpoints as this?
    /// </summary>
    /// <param name="otherEndpoints">The other endpoints.</param>
    /// <returns>True if the endpoints match.</returns>
    public bool HasTheSameEndpoints(RedisEndpoints otherEndpoints) 
        => otherEndpoints != null && Endpoints.SequenceEqual(otherEndpoints.Endpoints);

    /// <summary>
    /// Convert the endpoints to a string.
    /// </summary>
    /// <returns>The stringified endpoints.</returns>
    public override string ToString() => string.Join(",", Endpoints);
}
