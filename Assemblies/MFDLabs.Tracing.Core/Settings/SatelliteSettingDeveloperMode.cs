using System;

#pragma warning disable CS0067 // The event 'SatelliteSettingDeveloperMode.SettingsChanged' is never used

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

        public bool IsAllowListed(string serviceName)
        {
            return true;
        }

        public bool ShouldUseJaegerTracing(string serviceName)
        {
            return false;
        }

        public event EventHandler SettingsChanged;

        public double GetSamplingRate(string serviceName)
        {
            return 1.0;
        }
    }
}

#pragma warning restore CS0067 // The event 'SatelliteSettingDeveloperMode.SettingsChanged' is never used