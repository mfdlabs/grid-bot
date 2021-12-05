using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public interface IDefaultCircuitBreakerPolicyConfig
    {
        TimeSpan RetryInterval { get; }
        int FailuresAllowedBeforeTrip { get; }
    }
}
