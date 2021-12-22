using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Instrumentation;
using MFDLabs.Pipeline;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client.Monitoring
{
    public sealed class HttpRequestMetricsHandler : PipelineHandler<IHttpRequest, IHttpResponse>
    {
        public HttpRequestMetricsHandler(ICounterRegistry counterRegistry, string metricsCategoryName, string clientName)
        {
            if (metricsCategoryName.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Must be something like MFDLabs.Http.ServiceClient", nameof(metricsCategoryName));
            if (clientName.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Must identify the client like MyServiceClient", nameof(clientName));

            _clientMonitor = new Lazy<ClientRequestsMonitor>(() => ClientRequestsMonitor.GetOrCreate(counterRegistry, metricsCategoryName, clientName));
        }

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            var sw = RequestStarted(context.Input);
            try
            {
                base.Invoke(context);
                EvaluateResponse(context);
            }
            catch (Exception) { RequestFailed(context.Input); throw; }
            finally { RequestFinished(context.Input, sw); }
        }
        public override async Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            var sw = RequestStarted(context.Input);
            try
            {
                await base.InvokeAsync(context, cancellationToken);
                EvaluateResponse(context);
            }
            catch (Exception) { RequestFailed(context.Input); throw; }
            finally { RequestFinished(context.Input, sw); }
        }
        private void EvaluateResponse(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            if (context.Output.IsSuccessful)
            {
                RequestSucceeded(context.Input);
                return;
            }
            RequestFailed(context.Input);
        }
        private Stopwatch RequestStarted(IHttpRequest request)
        {
            _clientMonitor.Value.AddOutstandingRequest(request.Url.AbsolutePath);
            return Stopwatch.StartNew();
        }
        private void RequestFinished(IHttpRequest request, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            _clientMonitor.Value.AddResponseTime(request.Url.AbsolutePath, stopwatch);
            _clientMonitor.Value.RemoveOutstandingRequest(request.Url.AbsolutePath);
        }
        private void RequestSucceeded(IHttpRequest request)
        {
            _clientMonitor.Value.AddRequestSuccess(request.Url.AbsolutePath);
        }
        private void RequestFailed(IHttpRequest request)
        {
            _clientMonitor.Value.AddRequestFailure(request.Url.AbsolutePath);
        }

        private readonly Lazy<ClientRequestsMonitor> _clientMonitor;
    }
}
