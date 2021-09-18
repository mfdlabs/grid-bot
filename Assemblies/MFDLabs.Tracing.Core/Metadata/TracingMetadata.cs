using MFDLabs.Text.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTracing;
using OpenTracing.Noop;
using System;

#pragma warning disable CS0618 // Type or member is obsolete

namespace MFDLabs.Tracing.Core
{
    public class TracingMetadata
    {
        [Obsolete("Use and set LoggerFactory instead.", false)]
        public static ILogger<TracingMetadata> Logger { get; set; }

        public static ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

        public static string ServiceName { get; set; } = "UnnamedService";

        [Obsolete("This method will soon be deprecated. DO NOT USE!")]
        public static Func<bool> IsClientEnabled
        {
            private get
            {
                throw new NotImplementedException("Do not use this property");
            }
            set
            {
                throw new NotImplementedException("Do not use this property");
            }
        }

        public static Func<bool> IsMasterEnabled
        {
            private get
            {
                return _IsMasterEnabled;
            }
            set
            {
                _IsMasterEnabled = value ?? throw new ArgumentNullException("Attempt to set null for field _IsMasterEnabled");
                bool flag = IsTracingEnabled();
                if (flag)
                {
                    CheckFields();
                }
            }
        }

        public static Func<bool> IsTracingEnabled
        {
            get
            {
                return () => IsMasterEnabled() && SatelliteSettings.IsAllowListed(ServiceName);
            }
        }

        public static Func<double> MasterSamplingRate
        {
            private get
            {
                return _MasterSamplingRate;
            }
            set
            {
                _MasterSamplingRate = value ?? throw new ArgumentNullException("Attempt to set null for field _MasterSamplingRate");
            }
        }

        public static Func<double> SamplingRate
        {
            get
            {
                return () => SatelliteSettings.GetSamplingRate(ServiceName) * MasterSamplingRate();
            }
        }

        public static ISatelliteSettings SatelliteSettings { get; set; } = new SatelliteSettingDeveloperMode();

        public static void ConfigureTracerFactory(Func<string, ISatelliteSettings, ITracerFactory> tracerFactoryProvider)
        {
            if (tracerFactoryProvider == null)
            {
                throw new ArgumentNullException("tracerFactoryProvider cannot be null");
            }
            _TracerFactory = new Lazy<ITracerFactory>(() => tracerFactoryProvider(ServiceName, SatelliteSettings));
        }

        public static ITracingWrapper TracingWrapper
        {
            get
            {
                return IsTracingEnabled() ? _TracerFactory.Value.GetTracingWrapper() : _NoopTracingWrapper.Value;
            }
        }

        public static ITracer Tracer
        {
            get
            {
                return IsTracingEnabled() ? _TracerFactory.Value.GetTracer() : _NoopTracer.Value;
            }
        }

        public static ITracerFactory TracerFactory
        {
            get
            {
                return _TracerFactory.Value;
            }
        }

        private static void CheckFields()
        {
            if (ServiceName.IsNullOrWhiteSpace())
            {
                throw new NullReferenceException("ServiceName is null or empty or all whitespace in TracingMetadata");
            }
            if (SatelliteSettings == null)
            {
                throw new NullReferenceException("SatelliteSettings is null in TracingMetadata");
            }
            if (Logger == null)
            {
                throw new NullReferenceException("Logger is null in TracingMetadata");
            }
            if (_TracerFactory == null)
            {
                throw new NullReferenceException("_TracerFactory is null in TracingMetadata");
            }
        }

        private static Lazy<ITracerFactory> _TracerFactory;

        private static Func<bool> _IsMasterEnabled = () => false;

        private static Func<double> _MasterSamplingRate = () => 1.0;

        private static readonly Lazy<ITracer> _NoopTracer = new Lazy<ITracer>(NoopTracerFactory.Create);

        private static readonly Lazy<ITracingWrapper> _NoopTracingWrapper = new Lazy<ITracingWrapper>(() => NoopTracingWrapper.Instance);
    }
}

#pragma warning restore CS0618 // Type or member is obsolete