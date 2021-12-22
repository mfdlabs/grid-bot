using System;
using System.Collections.Generic;
using System.Linq;

namespace MFDLabs.RequestContext
{
    public class GlobalRequestContext : IRequestContext
    {
        private static string AuthenticatedUserIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}authenticated-userid";
        private static string AccountIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}account-id";
        private static string RequestIpAddressItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}request-ip-address";
        private static string UserAgentItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}user-agent";
        private static string AgeBracketItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}age-bracket";
        private static string RequestCountryCodeItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}request-country-code";
        private static string AccountCountryCodeItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}account-country-code";
        private static string PlatformTypeItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}platform-type";
        private static string EnvironmentAbbreviationItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}environment-abbreviation";
        private static string ApplicablePoliciesItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}applicable-policies";
        private static string TencentOpenIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}tencent-open-id";
        private static string TencentAccessTokenItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}tencent-access-token";
        private static string BrowserTrackerIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}browser-tracker-id";
        public long? AuthenticatedUserId { get; private set; }
        public long? AccountId { get; private set; }
        public string RequestIpAddress => this[RequestIpAddressItemKey];
        public string UserAgent => this[UserAgentItemKey];
        public AgeBracket? AgeBracket { get; private set; }
        public string RequestCountryCode => this[RequestCountryCodeItemKey];
        public string AccountCountryCode => this[AccountCountryCodeItemKey];
        public string PlatformType => this[PlatformTypeItemKey];
        public string EnvironmentAbbreviation => this[EnvironmentAbbreviationItemKey];
        public ICollection<Policy> ApplicablePolicies { get; } = new List<Policy>();
        public string TencentOpenId => this[TencentOpenIdItemKey];
        public string TencentAccessToken => this[TencentAccessTokenItemKey];
        public long? BrowserTrackerId { get; private set; }
        public string this[string key] => !_contextItems.ContainsKey(key) ? null : _contextItems[key];

        public GlobalRequestContext(long? authenticatedUserId = null,
            string requestIpAddress = null,
            string userAgent = null,
            AgeBracket? ageBracket = null,
            string requestCountryCode = null,
            string accountCountryCode = null,
            string platformType = null,
            string environmentAbbreviation = null,
            ICollection<Policy> applicablePolicies = null,
            ICollection<KeyValuePair<string, string>> additionalItems = null,
            string tencentOpenId = null,
            string tencentAccessToken = null,
            long? browserTrackerId = null,
            long? accountId = null)
        {
            AuthenticatedUserId = authenticatedUserId;
            AgeBracket = ageBracket;
            ApplicablePolicies = applicablePolicies ?? new List<Policy>();
            BrowserTrackerId = browserTrackerId;
            AccountId = accountId;
            var contextItems = new Dictionary<string, string>
            {
                {
                    AuthenticatedUserIdItemKey,
                    authenticatedUserId?.ToString()
                },
                {
                    AccountIdItemKey,
                    accountId?.ToString()
                },
                {
                    RequestIpAddressItemKey,
                    requestIpAddress
                },
                {
                    UserAgentItemKey,
                    userAgent
                },
                {
                    AgeBracketItemKey,
                    ageBracket?.ToString()
                },
                {
                    RequestCountryCode,
                    requestCountryCode
                },
                {
                    AccountCountryCodeItemKey,
                    accountCountryCode
                },
                {
                    PlatformTypeItemKey,
                    platformType
                },
                {
                    EnvironmentAbbreviationItemKey,
                    environmentAbbreviation
                },
                {
                    ApplicablePoliciesItemKey,
                    string.Join(",", ApplicablePolicies)
                },
                {
                    TencentOpenIdItemKey,
                    tencentOpenId
                },
                {
                    TencentAccessTokenItemKey,
                    tencentAccessToken
                },
                {
                    BrowserTrackerIdItemKey,
                    browserTrackerId?.ToString()
                }
            };
            if (additionalItems != null)
            {
                foreach (var kv in additionalItems)
                {
                    contextItems[kv.Key] = kv.Value;
                }
            }
            _contextItems = contextItems;
        }

        public GlobalRequestContext(IDictionary<string, string> contextItems)
        {
            _contextItems = contextItems ?? throw new ArgumentNullException(nameof(contextItems));
            LoadProperties();
        }

        private void LoadProperties()
        {
            if (!_contextItems.Any()) return;
            if (_contextItems.ContainsKey(AuthenticatedUserIdItemKey) &&
                _contextItems[AuthenticatedUserIdItemKey] != null &&
                long.TryParse(_contextItems[AuthenticatedUserIdItemKey],
                    out var authenticatedUserId)) 
                AuthenticatedUserId = authenticatedUserId;
            if (_contextItems.ContainsKey(AccountIdItemKey) &&
                _contextItems[AccountIdItemKey] != null &&
                long.TryParse(_contextItems[AccountIdItemKey],
                    out var accountId)) 
                AccountId = accountId;
            if (_contextItems.ContainsKey(AgeBracketItemKey) &&
                _contextItems[AgeBracketItemKey] != null &&
                Enum.TryParse<AgeBracket>(_contextItems[AgeBracketItemKey],
                    out var ageBracket)) 
                AgeBracket = ageBracket;
            if (_contextItems.ContainsKey(BrowserTrackerIdItemKey) &&
                _contextItems[BrowserTrackerIdItemKey] != null &&
                long.TryParse(_contextItems[BrowserTrackerIdItemKey],
                    out var browserTrackerId)) BrowserTrackerId = browserTrackerId;

            if (_contextItems[ApplicablePoliciesItemKey] == null) 
                return;
            var applicablePolicies = _contextItems[ApplicablePoliciesItemKey].Split(',');
            foreach (var t in applicablePolicies) 
                if (Enum.TryParse<Policy>(t, out var policy)) 
                    ApplicablePolicies.Add(policy);
        }

        public IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs() =>
            (from item in _contextItems
                select item).ToArray();

        private readonly IDictionary<string, string> _contextItems;
    }
}
