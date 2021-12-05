﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MFDLabs.RequestContext
{
    public class GlobalRequestContext : IRequestContext
    {
        internal static string AuthenticatedUserIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}authenticated-userid";
        internal static string AccountIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}account-id";
        internal static string RequestIPAddressItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}request-ip-address";
        internal static string UserAgentItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}user-agent";
        internal static string AgeBracketItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}age-bracket";
        internal static string RequestCountryCodeItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}request-country-code";
        internal static string AccountCountryCodeItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}account-country-code";
        internal static string PlatformTypeItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}platform-type";
        internal static string EnvironmentAbbreviationItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}environment-abbreviation";
        internal static string ApplicablePoliciesItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}applicable-policies";
        internal static string TencentOpenIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}tencent-open-id";
        internal static string TencentAccessTokenItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}tencent-access-token";
        internal static string BrowserTrackerIdItemKey => $"{GlobalRequestContextConstants.ContextItemKeyPrefix}browser-tracker-id";
        public long? AuthenticatedUserID { get; private set; }
        public long? AccountID { get; private set; }
        public string RequestIPAddress => this[RequestIPAddressItemKey];
        public string UserAgent => this[UserAgentItemKey];
        public AgeBracket? AgeBracket { get; private set; }
        public string RequestCountryCode => this[RequestCountryCodeItemKey];
        public string AccountCountryCode => this[AccountCountryCodeItemKey];
        public string PlatformType => this[PlatformTypeItemKey];
        public string EnvironmentAbbreviation => this[EnvironmentAbbreviationItemKey];
        public ICollection<Policy> ApplicablePolicies { get; } = new List<Policy>();
        public string TencentOpenId => this[TencentOpenIdItemKey];
        public string TencentAccessToken => this[TencentAccessTokenItemKey];
        public long? BrowserTrackerID { get; private set; }
        public string this[string key]
        {
            get
            {
                if (!_ContextItems.ContainsKey(key)) return null;
                return _ContextItems[key];
            }
        }

        public GlobalRequestContext(long? authenticatedUserID = null, string requestIPAddress = null, string userAgent = null, AgeBracket? ageBracket = null, string requestCountryCode = null, string accountCountryCode = null, string platformType = null, string environmentAbbreviation = null, ICollection<Policy> applicablePolicies = null, ICollection<KeyValuePair<string, string>> additionalItems = null, string tencentOpenId = null, string tencentAccessToken = null, long? browserTrackerID = null, long? accountId = null)
        {
            AuthenticatedUserID = authenticatedUserID;
            AgeBracket = ageBracket;
            ApplicablePolicies = applicablePolicies ?? new List<Policy>();
            BrowserTrackerID = browserTrackerID;
            AccountID = accountId;
            var contextItems = new Dictionary<string, string>
            {
                {
                    AuthenticatedUserIdItemKey,
                    authenticatedUserID?.ToString()
                },
                {
                    AccountIdItemKey,
                    accountId?.ToString()
                },
                {
                    RequestIPAddressItemKey,
                    requestIPAddress
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
                    browserTrackerID?.ToString()
                }
            };
            if (additionalItems != null)
            {
                foreach (var item in additionalItems)
                {
                    contextItems[item.Key] = item.Value;
                }
            }
            _ContextItems = contextItems;
        }

        public GlobalRequestContext(IDictionary<string, string> contextItems)
        {
            _ContextItems = contextItems ?? throw new ArgumentNullException("contextItems");
            LoadProperties();
        }

        private void LoadProperties()
        {
            if (!_ContextItems.Any())
            {
                return;
            }
            if (_ContextItems.ContainsKey(AuthenticatedUserIdItemKey) && _ContextItems[AuthenticatedUserIdItemKey] != null && long.TryParse(_ContextItems[AuthenticatedUserIdItemKey], out var authenticatedUserId))
            {
                AuthenticatedUserID = authenticatedUserId;
            }
            if (_ContextItems.ContainsKey(AccountIdItemKey) && _ContextItems[AccountIdItemKey] != null && long.TryParse(_ContextItems[AccountIdItemKey], out var accountId))
            {
                AccountID = accountId;
            }
            if (_ContextItems.ContainsKey(AgeBracketItemKey) && _ContextItems[AgeBracketItemKey] != null && Enum.TryParse<AgeBracket>(_ContextItems[AgeBracketItemKey], out var ageBracket))
            {
                AgeBracket = ageBracket;
            }
            if (_ContextItems.ContainsKey(BrowserTrackerIdItemKey) && _ContextItems[BrowserTrackerIdItemKey] != null && long.TryParse(_ContextItems[BrowserTrackerIdItemKey], out var browserTrackerId))
            {
                BrowserTrackerID = browserTrackerId;
            }
            if (_ContextItems[ApplicablePoliciesItemKey] != null)
            {
                var applicablePolicies = _ContextItems[ApplicablePoliciesItemKey].Split(',');
                for (int i = 0; i < applicablePolicies.Length; i++)
                {
                    if (Enum.TryParse<Policy>(applicablePolicies[i], out var policy))
                    {
                        ApplicablePolicies.Add(policy);
                    }
                }
            }
        }

        public ICollection<KeyValuePair<string, string>> ToKeyValuePairs()
        {
            return (from item in _ContextItems
                    select item).ToArray();
        }

        private readonly IDictionary<string, string> _ContextItems;
    }
}
