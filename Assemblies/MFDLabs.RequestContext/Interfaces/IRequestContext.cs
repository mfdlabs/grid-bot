using System.Collections.Generic;

namespace MFDLabs.RequestContext
{
    public interface IRequestContext
    {
        long? AuthenticatedUserId { get; }
        long? AccountId { get; }
        string RequestIpAddress { get; }
        string UserAgent { get; }
        AgeBracket? AgeBracket { get; }
        string RequestCountryCode { get; }
        string AccountCountryCode { get; }
        string PlatformType { get; }
        string EnvironmentAbbreviation { get; }
        ICollection<Policy> ApplicablePolicies { get; }
        string TencentOpenId { get; }
        string TencentAccessToken { get; }
        long? BrowserTrackerId { get; }
        string this[string key] { get; }
        
        IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs();
    }
}
