using MFDLabs.Http.Client;
using MFDLabs.Http.ServiceClient;
using MFDLabs.Sentinels.CircuitBreakerPolicy;
using System;

namespace MFDLabs.Users.Client
{
    internal sealed class UsersClientSettings : IServiceClientSettings, IHttpClientSettings, IDefaultCircuitBreakerPolicyConfig
    {
        public string ClientName
        {
            get
            {
                return "UsersClient";
            }
        }

        public string UserAgent
        {
            get
            {
                return "MFDLabs.Http.Client UsersHttpClient";
            }
        }

        public string Endpoint { get; }

        public int MaxRedirects { get; }

        public int FailuresAllowedBeforeTrip { get; }

        public TimeSpan RetryInterval { get; }

        public TimeSpan RequestTimeout { get; }

        public UsersClientSettings(UsersClientConfig config)
        {
            Endpoint = config.Url;
            MaxRedirects = config.MaxRedirects;
            RequestTimeout = config.RequestTimeout;
            FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip;
            RetryInterval = config.CircuitBreakerRetryInterval;
        }
    }
}
