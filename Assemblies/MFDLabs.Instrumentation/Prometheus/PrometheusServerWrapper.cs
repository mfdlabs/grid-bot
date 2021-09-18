using Prometheus;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace MFDLabs.Instrumentation.PrometheusListener
{
    public sealed class PrometheusServerWrapper
    {
        public bool UpdatingCountersEnabled { get; set; }

        public static PrometheusServerWrapper Instance
        {
            get
            {
                return _Instance.Value;
            }
        }

        private PrometheusServerWrapper()
        {
        }

        public string OutputMetricsRegistry()
        {
            string metricsRegistry;
            using (var stream = new MemoryStream())
            {
                Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, default);
                stream.Seek(0L, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    metricsRegistry = reader.ReadToEnd();
                }
            }
            return metricsRegistry;
        }

        public void RestartPromMetricsEndpoint(int port, string url = "metrics/", string hostname = null)
        {
            _MetricServer?.Stop();
            SetPort(port);
            _MetricServer = (hostname == null) ? new MetricServer(_CurrentPort, url, null, false) : new MetricServer(hostname, _CurrentPort, url, null, false);
            _MetricServer.Start();
        }

        public void RestartPromMetricsEndpoint(int portLowerBound, int portUpperBound, string url = "metrics/", string hostname = null)
        {
            _MetricServer?.Stop();
            DynamicallyPickPortInRangeOfPorts(portLowerBound, portUpperBound);
            _MetricServer = (hostname == null) ? new MetricServer(_CurrentPort, url, null, false) : new MetricServer(hostname, _CurrentPort, url, null, false);
            _MetricServer.Start();
        }

        public void StopMetricsEndpoint()
        {
            _MetricServer.Stop();
        }

        public int GetPort()
        {
            return _CurrentPort;
        }

        public void SetPort(int port)
        {
            _CurrentPort = port;
        }

        private bool DynamicallyPickPortInRangeOfPorts(int startPortRange, int endPortRange)
        {
            _MetricServer?.Stop();
            for (int port = startPortRange; port <= endPortRange; port++)
            {
                if (!PortInUse(port))
                {
                    _CurrentPort = port;
                    return true;
                }
            }
            return false;
        }

        private bool PortInUse(int port)
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any((IPEndPoint endPoint) => endPoint.Port == port);
        }

        private static readonly Lazy<PrometheusServerWrapper> _Instance = new Lazy<PrometheusServerWrapper>(() => new PrometheusServerWrapper());

        internal string MachineName = PrometheusConstants.EmptyVal;

        internal string HostIdentifier = PrometheusConstants.EmptyVal;

        internal string ServerFarmIdentifier = PrometheusConstants.EmptyVal;

        internal string SuperFarmIdentifier = PrometheusConstants.EmptyVal;

        private int _CurrentPort = -1;

        private MetricServer _MetricServer;
    }
}
