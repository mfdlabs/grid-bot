using System;

namespace MFDLabs.Tracing.Core
{
    public class SatelliteDockerSettings : ISatelliteSettings
    {
        public string Host => "host.docker.internal";
        public int Port => 8360;
        public string GrpcHost { get; } = "host.docker.internal";
        public int GrpcPort { get; } = 8361;
        public string Token => "developer";
        public int MaxBufferedSpans => 1000;
        public TimeSpan ReportPeriod => TimeSpan.FromSeconds(0.5);

        public event EventHandler SettingsChanged;

        public bool IsAllowListed(string serviceName) => true;
        public bool ShouldUseJaegerTracing(string serviceName) => false;
        public double GetSamplingRate(string serviceName) => 1;
    }
}