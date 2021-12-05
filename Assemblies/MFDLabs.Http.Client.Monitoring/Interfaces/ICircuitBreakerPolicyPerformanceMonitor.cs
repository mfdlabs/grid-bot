namespace MFDLabs.Http.Client.Monitoring
{
    public interface ICircuitBreakerPolicyPerformanceMonitor
    {
        void IncrementRequestsThatTripCircuitBreakerPerSecond();
        void IncrementRequestsTrippedByCircuitBreakerPerSecond();
    }
}
