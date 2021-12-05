using System;

namespace MFDLabs.Tracing.Core
{
    public interface ISatelliteSettings
    {
        string Host { get; }
        int Port { get; }
        string GrpcHost { get; }
        int GrpcPort { get; }
        string Token { get; }
        int MaxBufferedSpans { get; }
        TimeSpan ReportPeriod { get; }

        bool IsAllowListed(string serviceName);
        bool ShouldUseJaegerTracing(string serviceName);
        double GetSamplingRate(string serviceName);

        event EventHandler SettingsChanged;
    }
}
