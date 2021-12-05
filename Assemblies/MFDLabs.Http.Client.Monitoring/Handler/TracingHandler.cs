using System;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;
using MFDLabs.Tracing.Core;
using Microsoft.Extensions.Logging;

namespace MFDLabs.Http.Client.Monitoring
{
    public class TracingHandler : PipelineHandler<IHttpRequest, IHttpResponse>
    {
        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            TrySpanInjection(context);
            base.Invoke(context);
        }
        public override Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            TrySpanInjection(context);
            return base.InvokeAsync(context, cancellationToken);
        }
        private static void TrySpanInjection(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            try
            {
                if (TracingMetadata.IsTracingEnabled())
                    foreach (var header in TracingMetadata.TracingWrapper.ExtractSpanContextAsHttpHeaders()) 
                        context.Input.Headers.AddOrUpdate(header.Key, header.Value);
            }
            catch (Exception ex)
            {
                if (TracingMetadata.Logger != null) 
                    TracingMetadata.Logger.LogDebug("TracingHandler error while extracting span headers {0}", ex);
            }
        }
    }
}