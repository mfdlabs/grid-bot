using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public class DefaultCircuitBreakerPolicyConfig : IDefaultCircuitBreakerPolicyConfig
    {
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMilliseconds(250);
        public int FailuresAllowedBeforeTrip
        {
            get => _failuresAllowedBeforeTrip;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(FailuresAllowedBeforeTrip), "Has to be bigger than or equal to zero.");
                _failuresAllowedBeforeTrip = value;
            }
        }

        private int _failuresAllowedBeforeTrip;
    }
}
