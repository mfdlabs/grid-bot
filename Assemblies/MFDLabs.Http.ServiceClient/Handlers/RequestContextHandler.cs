using System;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;
using MFDLabs.RequestContext;

namespace MFDLabs.Http.ServiceClient
{
    public class RequestContextHandler : PipelineHandler<IHttpRequest, IHttpResponse>
    {
        public RequestContextHandler(IRequestContextLoader requestContextLoader) 
            => _requestContextLoader = requestContextLoader ?? throw new ArgumentNullException(nameof(requestContextLoader));

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            foreach (var contextItem in _requestContextLoader.GetCurrentContext().ToKeyValuePairs()) 
                context.Input.Headers.Add(contextItem.Key, contextItem.Value);
            base.Invoke(context);
        }
        public override Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            foreach (var contextItem in _requestContextLoader.GetCurrentContext().ToKeyValuePairs()) 
                context.Input.Headers.Add(contextItem.Key, contextItem.Value);
            return base.InvokeAsync(context, cancellationToken);
        }

        private readonly IRequestContextLoader _requestContextLoader;
    }
}
