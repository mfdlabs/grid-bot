using MFDLabs.Http.Client;

namespace MFDLabs.Http.ServiceClient
{
    public class ServiceOperationErrorException : HttpRequestFailedException
    {
        public string Code { get; set; }

        public ServiceOperationErrorException(IHttpResponse response, PayloadError payloadError)
            : base(response, GetExceptionMessage(response, payloadError))
        {
            Code = payloadError?.Code;
        }

        private static string GetExceptionMessage(IHttpResponse response, PayloadError payloadError)
        {
            return $"{BuildExceptionMessage(response)}\n\tError code: {payloadError?.Code}";
        }
    }
}
