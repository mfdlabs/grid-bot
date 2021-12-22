using System;
using MFDLabs.Http.ServiceClient;

namespace MFDLabs.Discord.RbxUsers.Client
{
    internal sealed class RbxDiscordUsersClientSettings : IServiceClientSettings
    {
        public string ClientName => "RbxDiscordUsersClient";
        public string UserAgent => "MFDLabs.Http.Client RbxDiscordUsersHttpClient";
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
