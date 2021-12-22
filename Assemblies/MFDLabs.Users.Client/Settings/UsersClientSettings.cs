using System;
using MFDLabs.Http.ServiceClient;

namespace MFDLabs.Users.Client
{
    internal sealed class UsersClientSettings : IServiceClientSettings
    {
        public string ClientName => "UsersClient";
        public string UserAgent => "MFDLabs.Http.Client UsersHttpClient";
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
