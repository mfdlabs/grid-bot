using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public abstract class TripReasonAuthorityBase<TExecutionContext> : ITripReasonAuthority<TExecutionContext>
    {
        public abstract bool IsReasonForTrip(TExecutionContext executionContext, Exception exception);
        protected static bool TryGetInnerExceptionOfType<T>(Exception exception, out T inner) 
            where T : Exception
        {
            inner = default;
            while (exception.InnerException != null)
            {
                inner = exception.InnerException as T;
                if (inner != null) return true;
            }
            return false;
        }
    }
}
