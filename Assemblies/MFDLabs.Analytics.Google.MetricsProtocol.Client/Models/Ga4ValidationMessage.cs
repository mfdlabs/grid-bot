namespace MFDLabs.Analytics.Google.MetricsProtocol.Client.Models;

using Newtonsoft.Json;

/// <summary>
/// Represents an error message from the GA4 validation service.
/// </summary>
public class Ga4ValidationMessage
{
    /// <summary>
    /// Where the invalid data was found.
    /// </summary>
    [JsonProperty("fieldPath")]
    public string FieldPath { get; set; }

    /// <summary>
    /// The description of the error.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    /// The simple name of the error.
    /// </summary>
    [JsonProperty("validationCode")]
    public string ValidationCode { get; set; } /* TODO: Maybe consider making this an enum? */
}