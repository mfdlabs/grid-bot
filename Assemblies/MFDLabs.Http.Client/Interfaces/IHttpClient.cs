using System;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.Client
{
    public interface IHttpClient : IDisposable
    {
        IHttpResponse Send(IHttpRequest request);

        Task<IHttpResponse> SendAsync(IHttpRequest request, CancellationToken cancellationToken);
    }
}
