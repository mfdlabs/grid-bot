using System.Collections.Generic;
using System.Text.RegularExpressions;
using Prometheus;

namespace Instrumentation.PrometheusListener
{
    internal class SummaryWrapper
    {
        public SummaryWrapper(string variableName, string instance, string category, string helpText, byte[] percentiles)
        {
            var sanitizedVariableName = variableName.Replace("{0}", "");
            sanitizedVariableName = Regex.Replace(sanitizedVariableName, PrometheusConstants.RegexReplacementChars, PrometheusConstants.ReplacementString);
            var sanitizedInstanceName = (instance == null) ? PrometheusConstants.EmptyVal : Regex.Replace(instance, PrometheusConstants.RegexReplacementChars, PrometheusConstants.ReplacementString);
            var sanitizedCategoryName = (category == null) ? PrometheusConstants.EmptyVal : Regex.Replace(category, PrometheusConstants.RegexReplacementChars, PrometheusConstants.ReplacementString);
            var objectives = new List<QuantileEpsilonPair>();
            foreach (var percentile in percentiles) objectives.Add(new QuantileEpsilonPair(percentile / 100, PrometheusConstants.MaxPercentileError));
            var summary = Metrics.CreateSummary(
                sanitizedVariableName,
                helpText,
                new SummaryConfiguration
                {
                    LabelNames = new string[]
                    {
                        "instance",
                        "category",
                        "machineName",
                        "host",
                        "ServerFarm",
                        "SuperFarm"
                    },
                    Objectives = objectives,
                    MaxAge = CounterReporter.SubmissionInterval
                }
            );
            _SummaryChild = summary.WithLabels(
                sanitizedInstanceName,
                sanitizedCategoryName,
                PrometheusServerWrapper.Instance.MachineName,
                PrometheusServerWrapper.Instance.HostIdentifier,
                PrometheusServerWrapper.Instance.ServerFarmIdentifier,
                PrometheusServerWrapper.Instance.SuperFarmIdentifier
            );
        }

        internal void AddDataPoint(double data)
        {
            if (PrometheusServerWrapper.Instance.UpdatingCountersEnabled) _SummaryChild.Observe(data);
        }

        private readonly Summary.Child _SummaryChild;
    }
}
