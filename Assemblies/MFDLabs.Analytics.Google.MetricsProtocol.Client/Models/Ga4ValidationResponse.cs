namespace MFDLabs.Analytics.Google.MetricsProtocol.Client.Models;

using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Represents the response from /debug/mp/collect
/// </summary>
public class Ga4ValidationResponse
{
    /// <summary>
    /// The validation message array.
    /// </summary>
    [JsonProperty("validationMessages")]
    public ICollection<Ga4ValidationMessage> ValidationMessages { get; set; }
}