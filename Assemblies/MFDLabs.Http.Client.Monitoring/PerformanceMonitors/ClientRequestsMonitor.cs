using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client.Monitoring
{
    public class ClientRequestsMonitor
    {
        private ClientRequestsMonitor(ICounterRegistry counterRegistry, string globalCategoryName, string clientName)
        {
            if (globalCategoryName.IsNullOrWhiteSpace()) throw new ArgumentException("Must be something like MFDLabs.Http.ServiceClient", nameof(globalCategoryName));
            if (clientName.IsNullOrWhiteSpace()) throw new ArgumentException("Must identify the client like MyServiceClient", nameof(clientName));

            _counterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
            _clientName = clientName;
            _globalCategoryName = globalCategoryName;
            _category = $"{globalCategoryName}.{clientName}";
            InitializeCounters();
        }

        private PerInstancePerformanceMonitor TotalActionMonitor => GetOrCreateAction(TotalInstanceName);

        public static ClientRequestsMonitor GetOrCreate(ICounterRegistry counterRegistry, string metricsCategoryName, string clientName)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
            if (metricsCategoryName.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Metrics category should be a dot separated namespace", nameof(metricsCategoryName));
            if (clientName.IsNullOrWhiteSpace()) throw new ArgumentException("Client name should be single word without spaces", nameof(clientName));
            return ClientMonitors.GetOrAdd(clientName, _ => new ClientRequestsMonitor(counterRegistry, metricsCategoryName, clientName));
        }
        public void AddRequestFailure(string actionPath)
        {
            TotalActionMonitor.FailuresPerSecond.Increment();
            GetOrCreateAction(actionPath).FailuresPerSecond.Increment();
            _failuresPerSecond.Increment();
        }
        public void AddRequestSuccess(string actionPath)
        {
            TotalActionMonitor.SuccessesPerSecond.Increment();
            GetOrCreateAction(actionPath).SuccessesPerSecond.Increment();
            _successesPerSecond.Increment();
        }
        public void AddResponseTime(string actionPath, Stopwatch duration)
        {
            double totalMilliseconds = duration.Elapsed.TotalMilliseconds;
            TotalActionMonitor.AverageResponseTime.Sample(totalMilliseconds);
            GetOrCreateAction(actionPath).AverageResponseTime.Sample(totalMilliseconds);
            _averageResponseTime.Sample(duration.ElapsedMilliseconds);
            _percentileResponseTime.Sample(duration.ElapsedMilliseconds);
        }
        public void AddOutstandingRequest(string actionPath)
        {
            TotalActionMonitor.RequestsOutstanding.Increment();
            GetOrCreateAction(actionPath).RequestsOutstanding.Increment();
            GetApplicationRequestRequestCountDictionary()?.AddOrUpdate(actionPath, 1, (_, count) => count + 1);
        }
        public void RemoveOutstandingRequest(string actionPath)
        {
            TotalActionMonitor.RequestsOutstanding.Decrement();
            GetOrCreateAction(actionPath).RequestsOutstanding.Decrement();
        }
        private void InitializeCounters()
        {
            GetOrCreateAction(TotalInstanceName);
            _failuresPerSecond = _counterRegistry.GetRateOfCountsPerSecondCounter(_globalCategoryName, FailuresPerSecondCounterName, _clientName);
            _successesPerSecond = _counterRegistry.GetRateOfCountsPerSecondCounter(_globalCategoryName, SuccessesPerSecondCounterName, _clientName);
            _averageResponseTime = _counterRegistry.GetAverageValueCounter(_globalCategoryName, AverageResponseTimeCounterName, _clientName);
            _percentileResponseTime = _counterRegistry.GetPercentileCounter(_globalCategoryName, "ResponseTime.Percentile.{0}", Percentiles, _clientName);
        }
        private PerInstancePerformanceMonitor GetOrCreateAction(string actionName) 
            => _actionMonitors.GetOrAdd(
                actionName.IsNullOrEmpty() ? "(root)" : actionName,
                _ => new PerInstancePerformanceMonitor(_counterRegistry, _category, actionName)
            );
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, int>> GetApplicationRequestRequestCountDictionaries()
        {
            var requestCache = RequestCacheDictionaryGetter?.Invoke();
            if (requestCache == null) return null;
            if (requestCache.Contains(PerApplicationRequestRequestsCounterDictionaryKey)) 
                return requestCache[PerApplicationRequestRequestsCounterDictionaryKey] as ConcurrentDictionary<string, ConcurrentDictionary<string, int>>;
            var perAppRequestCounterCache = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            requestCache[PerApplicationRequestRequestsCounterDictionaryKey] = perAppRequestCounterCache;
            return perAppRequestCounterCache;
        }
        private ConcurrentDictionary<string, int> GetApplicationRequestRequestCountDictionary() 
            => GetApplicationRequestRequestCountDictionaries()?.GetOrAdd(_clientName, _ => new ConcurrentDictionary<string, int>());

        private const string FailuresPerSecondCounterName = "Failures/s";
        private const string SuccessesPerSecondCounterName = "Requests/s";
        private const string AverageResponseTimeCounterName = "Average Response Time";
        private const string TotalInstanceName = "_Total";
        private const string PerApplicationRequestRequestsCounterDictionaryKey = "ClientRequestsMonitor:ApplicationRequest_TotalRequests";
        private static readonly byte[] Percentiles = new byte[] { 25, 50, 75, 95, 99 };
        private static readonly ConcurrentDictionary<string, ClientRequestsMonitor> ClientMonitors = new();
        private readonly ICounterRegistry _counterRegistry;
        private readonly ConcurrentDictionary<string, PerInstancePerformanceMonitor> _actionMonitors = new();
        private static readonly Func<IDictionary> RequestCacheDictionaryGetter = null;
        private readonly string _globalCategoryName;
        private readonly string _category;
        private readonly string _clientName;
        private IAverageValueCounter _averageResponseTime;
        private IRateOfCountsPerSecondCounter _failuresPerSecond;
        private IPercentileCounter _percentileResponseTime;
        private IRateOfCountsPerSecondCounter _successesPerSecond;
    }
}
