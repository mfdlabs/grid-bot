using MFDLabs.Http.Client;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.ServiceClient
{
    public interface IServiceRequestSender : IHttpRequestSender
    {
        TResponse SendPostRequest<TRequest, TResponse>(string path, TRequest requestData);

        Task<TResponse> SendPostRequestAsync<TRequest, TResponse>(string path, TRequest requestData, CancellationToken cancellationToken);
    }
}
