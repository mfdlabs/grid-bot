namespace MFDLabs.Analytics.Google.MetricsProtocol.Client.Models;

using System.Collections.Generic;
using Newtonsoft.Json;

#nullable enable

/// <summary>
/// This class is a wrapper for event information.
/// It is only ingested by the Google Analytics client.
/// </summary>
public class Ga4EventRequest
{
    [JsonProperty("clientId")]
    public string ClientId { get; set; } = "";

    [JsonProperty("userId")]
    public string? UserId { get; set; }

    [JsonProperty("timestampMicros")]
    public long? TimestampMicros { get; set; }

    [JsonProperty("userProperties")]
    public object? UserProperties { get; set; }

    [JsonProperty("nonPersonalizedAds")]
    public bool? NonPersonalizedAds { get; set; }

    [JsonProperty("events")]
    public ICollection<object> Events { get; set; } = new List<object>();
}