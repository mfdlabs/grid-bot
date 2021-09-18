using Polly.CircuitBreaker;
using Polly.Wrap;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.Client.Monitoring
{
    public static class ClientPollyPolicyExtensions
    {
        public static Task ExecutePolicy(this AsyncPolicyWrap policyWrap, Func<CancellationToken, Task> method, Action<BrokenCircuitException> onCircuitException, CancellationToken cancellationToken)
        {
            return policyWrap.ExecutePolicy(method, onCircuitException, cancellationToken);
        }

        public static Task<T> ExecutePolicy<T>(this AsyncPolicyWrap policyWrap, Func<CancellationToken, Task<T>> method, Action<BrokenCircuitException> onCircuitException, CancellationToken cancellationToken)
        {
            return policyWrap.ExecutePolicy(method, onCircuitException, cancellationToken);
        }
    }
}
