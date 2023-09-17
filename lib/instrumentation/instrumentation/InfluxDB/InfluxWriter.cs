using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Text.Extensions;

namespace Instrumentation
{
    internal class InfluxWriter
    {
        internal InfluxWriter(Action<Exception> exceptionHandler) 
            => _ExceptionHandler = exceptionHandler ?? throw new ArgumentException("exceptionHandler");

        public void Persist(ICollectionConfiguration configuration, IReadOnlyCollection<KeyValuePair<CounterKey, double>> datapoints)
        {
            if (datapoints.Count == 0) return;
            var groupByEndpoint = GroupByEndpoint(configuration, datapoints);
            try
            {
                foreach (var group in groupByEndpoint)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var counterKey in group.Value)
                    {
                        builder.Append("perfmon,machine=");
                        builder.Append(EscapeTagName(configuration.HostIdentifier));
                        builder.Append(",category=");
                        builder.Append(EscapeTagName(counterKey.Key.Category));
                        builder.Append(",counter=");
                        builder.Append(EscapeTagName(counterKey.Key.Name));
                        if (!counterKey.Key.Instance.IsNullOrEmpty())
                        {
                            builder.Append(",instance=");
                            builder.Append(EscapeTagName(counterKey.Key.Instance));
                        }
                        if (!configuration.FarmIdentifier.IsNullOrEmpty())
                        {
                            builder.Append(",farm=");
                            builder.Append(EscapeTagName(configuration.FarmIdentifier));
                        }
                        if (!configuration.SuperFarmIdentifier.IsNullOrEmpty())
                        {
                            builder.Append(",superFarm=");
                            builder.Append(EscapeTagName(configuration.SuperFarmIdentifier));
                        }
                        builder.Append(" value=");
                        builder.Append(counterKey.Value);
                        builder.Append('\n');
                    }
                    try
                    {
                        using (var client = new ExtendedWebClient())
                            client.UploadStringGzipped(
                                $"{group.Key}/write?db={configuration.InfluxDatabaseName}&precision=s",
                                builder.ToString(),
                                configuration.InfluxCredentials?.Username,
                                configuration.InfluxCredentials?.Password
                            );
                    }
                    catch (WebException ex) { _ExceptionHandler(CreateDetailedException(ex, group.Key)); }
                    catch (Exception ex) { _ExceptionHandler(ex); }
                }
            }
            catch (Exception ex) { try { _ExceptionHandler(ex); } catch { } }
        }
        internal Dictionary<string, List<KeyValuePair<CounterKey, double>>> GroupByEndpoint(ICollectionConfiguration configuration, IEnumerable<KeyValuePair<CounterKey, double>> datapoints)
        {
            var groupsByEndpoint = new Dictionary<string, List<KeyValuePair<CounterKey, double>>>();
            foreach (var datapoint in datapoints)
            {
                foreach (var key in configuration.GetInfluxEndpointsForCategory(datapoint.Key.Category))
                {
                    if (!groupsByEndpoint.TryGetValue(key, out var group))
                    {
                        group = new List<KeyValuePair<CounterKey, double>>();
                        groupsByEndpoint[key] = group;
                    }
                    group.Add(datapoint);
                }
            }
            return groupsByEndpoint;
        }
        private static Exception CreateDetailedException(WebException ex, string baseUrl)
        {
            try
            {
                var response = ex.Response?.GetResponseStream();

                string responseBody = null;
                if (response != null) 
                    using (var reader = new StreamReader(response)) 
                        responseBody = reader.ReadToEnd();
                return new Exception(string.Format("Failed to write to InfluxDB server {0}. Response body = {1}. Status = {2}", baseUrl, responseBody, ex.Status), ex);
            }
            catch { return ex; }
        }
        private static string EscapeTagName(string stringToEscape)
        {
            stringToEscape = stringToEscape.Replace(" ", "\\ ");
            stringToEscape = stringToEscape.Replace(",", "\\,");
            stringToEscape = stringToEscape.Replace("=", "\\=");
            return stringToEscape;
        }

        private readonly Action<Exception> _ExceptionHandler;
    }
}
