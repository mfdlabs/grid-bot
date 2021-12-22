using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Http.Client;

namespace MFDLabs.Http.ServiceClient
{
    public class ServiceRequestSender : HttpRequestSender, IServiceRequestSender
    {
        public ServiceRequestSender(IHttpClient httpClient, IHttpRequestBuilder httpRequestBuilder)
            : base(httpClient, httpRequestBuilder)
        { }

        public TResponse SendPostRequest<TRequest, TResponse>(string path, TRequest requestData)
            => SendRequestWithJsonBody<TRequest, Payload<TResponse>>(HttpMethod.Post, path, requestData).Data;
        public async Task<TResponse> SendPostRequestAsync<TRequest, TResponse>(string path, TRequest requestData, CancellationToken cancellationToken)
            => (await SendRequestWithJsonBodyAsync<TRequest, Payload<TResponse>>(HttpMethod.Post, path, requestData, cancellationToken)).Data;
    }
}
