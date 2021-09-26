using System.Net;
using MFDLabs.Http.Client;

namespace MFDLabs.Http.ServiceClient
{
    public class OperationErrorHandler : RequestFailureThrowsHandler
    {
        protected override void CheckResponse(IHttpResponse httpResponse, IHttpRequest httpRequest)
        {
            if (httpResponse == null || httpRequest == null)
            {
                return;
            }
            if (IsOperationError(httpResponse))
            {
                var payload = httpResponse.GetJsonBody<Payload<object>>();
                if (payload?.Error != null)
                {
                    throw new ServiceOperationErrorException(httpResponse, payload.Error);
                }
            }
            base.CheckResponse(httpResponse, httpRequest);
        }

        private bool IsOperationError(IHttpResponse httpResponse)
        {
            return httpResponse != null && httpResponse.StatusCode == _ServiceFailureStatusCode && httpResponse.Headers.ContentType != null && httpResponse.Headers.ContentType.StartsWith(_JsonContentType);
        }

        private const HttpStatusCode _ServiceFailureStatusCode = HttpStatusCode.Conflict;

        private const string _JsonContentType = "application/json";
    }
}
