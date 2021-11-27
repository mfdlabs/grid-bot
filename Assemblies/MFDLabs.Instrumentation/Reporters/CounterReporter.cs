using System;
using System.Collections.Generic;
using System.Threading;
using MFDLabs.Instrumentation.Infrastructure;
using MFDLabs.Instrumentation.PrometheusListener;

namespace MFDLabs.Instrumentation
{
    public sealed class CounterReporter : IDisposable, ICounterReporter
    {
        public CounterReporter(ICounterRegistry counterRegistry, Action<Exception> exceptionHandler, string machineName = null)
            : this(counterRegistry, exceptionHandler, new InfrastructureServiceConfigurationProvider(machineName, exceptionHandler))
        {
            PrometheusServerWrapper.Instance.MachineName = (machineName ?? PrometheusConstants.EmptyVal);
        }

        public CounterReporter(ICounterRegistry counterRegistry, Action<Exception> exceptionHandler, IConfigurationProvider configurationProvider)
        {
            _CounterRegistry = counterRegistry ?? throw new ArgumentNullException("counterRegistry");
            _ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException("exceptionHandler");
            _ConfigurationProvider = configurationProvider ?? throw new ArgumentNullException("configurationProvider");
            _InfluxWriter = new InfluxWriter(exceptionHandler);
            _Timer = new Timer((state) =>
            {
                PersistCounterValues();
            });
        }

        public void Start()
        {
            _Timer.Change(SubmissionInterval, SubmissionInterval);
        }

        public void Dispose()
        {
            (_ConfigurationProvider as IDisposable)?.Dispose();
            _Timer.Dispose();
        }

        public static CounterReporter CreateAndStart(ICounterRegistry counterRegistry, Action<Exception> exceptionHandler)
        {
            var reporter = new CounterReporter(counterRegistry, exceptionHandler);
            reporter.Start();
            return reporter;
        }

        public static CounterReporter CreateAndStart(ICounterRegistry counterRegistry, Action<Exception> exceptionHandler, IConfigurationProvider configurationProvider)
        {
            var reporter = new CounterReporter(counterRegistry, exceptionHandler, configurationProvider);
            reporter.Start();
            return reporter;
        }

        internal void PersistCounterValues()
        {
            try
            {
                var configuration = _ConfigurationProvider.GetConfiguration();
                if (configuration != null)
                {
                    var flushedCounters = _CounterRegistry.FlushCounters();
                    var counters = new List<KeyValuePair<CounterKey, double>>(flushedCounters.Count + 1);
                    counters.AddRange(flushedCounters);
                    counters.Add(new KeyValuePair<CounterKey, double>(_NumberOfDataPointsSentCounterKey, counters.Count));
                    _InfluxWriter.Persist(configuration, counters);
                    PrometheusServerWrapper.Instance.HostIdentifier = (configuration.HostIdentifier ?? PrometheusConstants.EmptyVal);
                    PrometheusServerWrapper.Instance.ServerFarmIdentifier = (configuration.FarmIdentifier ?? PrometheusConstants.EmptyVal);
                    PrometheusServerWrapper.Instance.SuperFarmIdentifier = (configuration.SuperFarmIdentifier ?? PrometheusConstants.EmptyVal);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _ExceptionHandler(ex);
                }
                catch
                {
                }
            }
        }

        internal static readonly TimeSpan SubmissionInterval = TimeSpan.FromSeconds(30.0);
        private static readonly CounterKey _NumberOfDataPointsSentCounterKey = new CounterKey("MFDLabs.Instrumentation", "NumberOfDataPointsSent", null);
        private readonly ICounterRegistry _CounterRegistry;
        private readonly Action<Exception> _ExceptionHandler;
        private readonly IConfigurationProvider _ConfigurationProvider;
        private readonly Timer _Timer;
        private readonly InfluxWriter _InfluxWriter;
    }
}
