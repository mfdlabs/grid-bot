using System;

namespace MFDLabs.Tracing.Core
{
    public class SatelliteSettingDeveloperMode : ISatelliteSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 8360;
        public string GrpcHost { get; } = "localhost";
        public int GrpcPort { get; } = 8361;
        public string Token { get; set; } = "developer";
        public int MaxBufferedSpans { get; set; } = 1;
        public TimeSpan ReportPeriod { get; set; } = TimeSpan.FromSeconds(0.5);

        public event EventHandler SettingsChanged;

        public bool IsAllowListed(string serviceName) => true;
        public bool ShouldUseJaegerTracing(string serviceName) => false;
        public double GetSamplingRate(string serviceName) => 1;
    }
}
