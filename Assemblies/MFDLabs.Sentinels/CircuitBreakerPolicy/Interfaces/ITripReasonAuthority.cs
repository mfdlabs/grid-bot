using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public interface ITripReasonAuthority<in TExecutionContext>
    {
        bool IsReasonForTrip(TExecutionContext executionContext, Exception exception);
    }
}
