using MFDLabs.Http.Client;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.ServiceClient
{
    public class ServiceRequestSender : HttpRequestSender, IServiceRequestSender, IHttpRequestSender
    {
        public ServiceRequestSender(IHttpClient httpClient, IHttpRequestBuilder httpRequestBuilder)
            : base(httpClient, httpRequestBuilder)
        {
        }

        public TResponse SendPostRequest<TRequest, TResponse>(string path, TRequest requestData)
        {
            return SendRequestWithJsonBody<TRequest, Payload<TResponse>>(HttpMethod.Post, path, requestData, null).Data;
        }

        public async Task<TResponse> SendPostRequestAsync<TRequest, TResponse>(string path, TRequest requestData, CancellationToken cancellationToken)
        {
            return (await SendRequestWithJsonBodyAsync<TRequest, Payload<TResponse>>(HttpMethod.Post, path, requestData, cancellationToken, null)).Data;
        }
    }
}
