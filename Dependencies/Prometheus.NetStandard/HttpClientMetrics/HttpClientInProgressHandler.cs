using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.HttpClientMetrics
{
    internal sealed class HttpClientInProgressHandler : HttpClientDelegatingHandlerBase<ICollector<IGauge>, IGauge>
    {
        public HttpClientInProgressHandler(HttpClientInProgressOptions? options, HttpClientIdentity identity)
            : base(options, options?.Gauge, identity)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (CreateChild(request, null).TrackInProgress())
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }

        protected override string[] DefaultLabels => HttpClientRequestLabelNames.KnownInAdvance;

        protected override ICollector<IGauge> CreateMetricInstance(string[] labelNames) => MetricFactory.CreateGauge(
            "httpclient_requests_in_progress",
            "Number of requests currently being executed by an HttpClient.",
            new GaugeConfiguration
            {
                LabelNames = labelNames
            });
    }
}