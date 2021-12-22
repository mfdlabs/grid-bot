using System;
using System.Collections.Generic;
using System.Net;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.Client
{
    public class DefaultTripReasonAuthority : TripReasonAuthorityBase<IExecutionContext<IHttpRequest, IHttpResponse>>
    {
        public override bool IsReasonForTrip(IExecutionContext<IHttpRequest, IHttpResponse> executionContext, Exception exception)
        {
            switch (exception)
            {
                case null:
                    return false;
                case HttpException when TryGetInnerExceptionOfType<WebException>(exception, out var ex):
                    return WebExceptionStatusesToTripOn.Contains(ex.Status);
                default:
                {
                    if (executionContext?.Output != null && !executionContext.Output.IsSuccessful)
                        return HttpStatusCodesToTripOn.Contains(executionContext.Output.StatusCode);
                    break;
                }
            }

            return false;
        }

        private static readonly ISet<HttpStatusCode> HttpStatusCodesToTripOn = new HashSet<HttpStatusCode>
        {
            HttpStatusCode.BadGateway,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.RequestTimeout,
        };

        private static readonly ISet<WebExceptionStatus> WebExceptionStatusesToTripOn = new HashSet<WebExceptionStatus>
        {
            WebExceptionStatus.ConnectFailure,
            WebExceptionStatus.ConnectionClosed,
            WebExceptionStatus.KeepAliveFailure,
            WebExceptionStatus.NameResolutionFailure,
            WebExceptionStatus.ReceiveFailure,
            WebExceptionStatus.SendFailure,
            WebExceptionStatus.ProxyNameResolutionFailure,
            WebExceptionStatus.RequestCanceled,
            WebExceptionStatus.Timeout
        };
    }
}
