namespace MFDLabs.Analytics.Google.MetricsProtocol.Client.Models;

using Newtonsoft.Json;

/// <summary>
/// Represents a Google Analytics Metrics protocol event as defined in
/// https://developers.google.com/analytics/devguides/collection/protocol/v1/reference#event
/// </summary>
public class Ga4Event
{
    /// <summary>
    /// <b>Required</b>. The name for the event.
    ///
    /// See the <a href="https://developers.google.com/analytics/devguides/collection/protocol/ga4/reference/events">events</a> reference for all options.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// <b>Optional</b>. The parameters for the event.
    ///
    /// See the <a href="https://developers.google.com/analytics/devguides/collection/protocol/ga4/reference/events">events</a> for the suggested parameters for each event.
    /// </summary>
    [JsonProperty("params")]
    public object Params { get; set; }
}