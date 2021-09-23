using System;
using MFDLabs.Http.Client;
using MFDLabs.Http.ServiceClient;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Discord.RbxUsers.Client
{
    internal sealed class RbxDiscordUsersClientSettings : IServiceClientSettings, IHttpClientSettings, IDefaultCircuitBreakerPolicyConfig
    {
        public string ClientName
        {
            get
            {
                return "RbxDiscordUsersClient";
            }
        }

        public string UserAgent
        {
            get
            {
                return "MFDLabs.Http.Client RbxDiscordUsersHttpClient";
            }
        }

        public string Endpoint { get; }

        public int MaxRedirects { get; }

        public int FailuresAllowedBeforeTrip { get; }

        public TimeSpan RetryInterval { get; }

        public TimeSpan RequestTimeout { get; }

        public RbxDiscordUsersClientSettings(RbxDiscordUsersClientConfig config)
        {
            Endpoint = config.Url;
            MaxRedirects = config.MaxRedirects;
            RequestTimeout = config.RequestTimeout;
            FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip;
            RetryInterval = config.CircuitBreakerRetryInterval;
        }
    }
}
