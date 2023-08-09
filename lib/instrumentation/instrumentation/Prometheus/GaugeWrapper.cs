using System.Text.RegularExpressions;
using Prometheus;

namespace MFDLabs.Instrumentation.PrometheusListener
{
    internal class GaugeWrapper
    {
        public GaugeWrapper(string variableName, string instance, string category, string helpText)
        {
            var sanitizedName = Regex.Replace(variableName, PrometheusConstants.RegexReplacementChars, PrometheusConstants.ReplacementString);
            var sanitizedInstanceName = (instance == null) ? PrometheusConstants.EmptyVal : Regex.Replace(instance, PrometheusConstants.RegexReplacementChars, PrometheusConstants.ReplacementString);
            var sanitizedCategoryName = (category == null) ? PrometheusConstants.EmptyVal : Regex.Replace(category, PrometheusConstants.RegexReplacementChars, PrometheusConstants.ReplacementString);
            var gauge = Metrics.CreateGauge(
                sanitizedName,
                helpText,
                new GaugeConfiguration
                {
                    LabelNames = new[]
                    {
                        "instance",
                        "category",
                        "machineName",
                        "host",
                        "serverFarm",
                        "superFarm"
                    }
                }
            );
            _GaugeChild = gauge.WithLabels(
                sanitizedInstanceName,
                sanitizedCategoryName,
                PrometheusServerWrapper.Instance.MachineName,
                PrometheusServerWrapper.Instance.HostIdentifier,
                PrometheusServerWrapper.Instance.ServerFarmIdentifier,
                PrometheusServerWrapper.Instance.SuperFarmIdentifier
            );
        }

        internal void Set(double val)
        {
            if (PrometheusServerWrapper.Instance.UpdatingCountersEnabled) _GaugeChild.Set(val);
        }
        internal void Inc(double val)
        {
            if (PrometheusServerWrapper.Instance.UpdatingCountersEnabled) _GaugeChild.Inc(val);
        }
        internal void Dec(double val)
        {
            if (PrometheusServerWrapper.Instance.UpdatingCountersEnabled) _GaugeChild.Dec(val);
        }

        private readonly Gauge.Child _GaugeChild;
    }
}
