namespace Prometheus.HttpClientMetrics
{
    public sealed class HttpClientRequestCountOptions : HttpClientMetricsOptionsBase
    {
        /// <summary>
        /// Set this to use a custom metric instead of the default.
        /// </summary>
        public ICollector<ICounter>? Counter { get; set; }
    }
}