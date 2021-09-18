using System;

#pragma warning disable CS0067 // The event 'SatelliteSettingDeveloperMode.SettingsChanged' is never used

namespace MFDLabs.Tracing.Core
{
    public class SatelliteDockerSettings : ISatelliteSettings
    {
        public string Host
        {
            get
            {
                return "host.docker.internal";
            }
        }

        public int Port
        {
            get
            {
                return 8360;
            }
        }

        public string GrpcHost { get; } = "host.docker.internal";

        public int GrpcPort { get; } = 8361;

        public string Token
        {
            get
            {
                return "developer";
            }
        }

        public int MaxBufferedSpans
        {
            get
            {
                return 1000;
            }
        }

        public TimeSpan ReportPeriod
        {
            get
            {
                return TimeSpan.FromSeconds(0.5);
            }
        }

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

#pragma warning restore CS0067 // The event 'SatelliteDockerSettings.SettingsChanged' is never used