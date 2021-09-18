using MFDLabs.Http.Client;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.ServiceClient
{
    public interface IServiceClientSettings : IHttpClientSettings, IDefaultCircuitBreakerPolicyConfig
    {
        string Endpoint { get; }

        string ClientName { get; }
    }
}
