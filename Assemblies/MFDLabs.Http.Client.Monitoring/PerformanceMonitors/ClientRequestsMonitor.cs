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
            if (globalCategoryName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Must be something like MFDLabs.Http.ServiceClient", "globalCategoryName");
            }
            if (clientName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Must identify the client like MyServiceClient", "clientName");
            }

            _CounterRegistry = counterRegistry ?? throw new ArgumentNullException("counterRegistry");
            _ClientName = clientName;
            _GlobalCategoryName = globalCategoryName;
            _Category = $"{globalCategoryName}.{clientName}";
            InitializeCounters();
        }

        private PerInstancePerformanceMonitor TotalActionMonitor
        {
            get
            {
                return GetOrCreateAction(_TotalInstanceName);
            }
        }

        public static ClientRequestsMonitor GetOrCreate(ICounterRegistry counterRegistry, string metricsCategoryName, string clientName)
        {
            if (counterRegistry == null)
            {
                throw new ArgumentNullException("counterRegistry");
            }
            if (metricsCategoryName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Metrics category should be a dot separated namespace", "metricsCategoryName");
            }
            if (clientName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Client name should be single word without spaces", "clientName");
            }
            return _ClientMonitors.GetOrAdd(clientName, (counter) => new ClientRequestsMonitor(counterRegistry, metricsCategoryName, clientName));
        }

        public void AddRequestFailure(string actionPath)
        {
            TotalActionMonitor.FailuresPerSecond.Increment();
            GetOrCreateAction(actionPath).FailuresPerSecond.Increment();
            _FailuresPerSecond.Increment();
        }

        public void AddRequestSuccess(string actionPath)
        {
            TotalActionMonitor.SuccessesPerSecond.Increment();
            GetOrCreateAction(actionPath).SuccessesPerSecond.Increment();
            _SuccessesPerSecond.Increment();
        }

        public void AddResponseTime(string actionPath, Stopwatch duration)
        {
            double totalMilliseconds = duration.Elapsed.TotalMilliseconds;
            TotalActionMonitor.AverageResponseTime.Sample(totalMilliseconds);
            GetOrCreateAction(actionPath).AverageResponseTime.Sample(totalMilliseconds);
            _AverageResponseTime.Sample(duration.ElapsedMilliseconds);
            _PercentileResponseTime.Sample(duration.ElapsedMilliseconds);
        }

        public void AddOutstandingRequest(string actionPath)
        {
            TotalActionMonitor.RequestsOutstanding.Increment();
            GetOrCreateAction(actionPath).RequestsOutstanding.Increment();
            GetApplicationRequestRequestCountDictionary()?.AddOrUpdate(actionPath, 1, (path, count) => count + 1);
        }

        public void RemoveOutstandingRequest(string actionPath)
        {
            TotalActionMonitor.RequestsOutstanding.Decrement();
            GetOrCreateAction(actionPath).RequestsOutstanding.Decrement();
        }

        private void InitializeCounters()
        {
            GetOrCreateAction(_TotalInstanceName);
            _FailuresPerSecond = _CounterRegistry.GetRateOfCountsPerSecondCounter(_GlobalCategoryName, _FailuresPerSecondCounterName, _ClientName);
            _SuccessesPerSecond = _CounterRegistry.GetRateOfCountsPerSecondCounter(_GlobalCategoryName, _SuccessesPerSecondCounterName, _ClientName);
            _AverageResponseTime = _CounterRegistry.GetAverageValueCounter(_GlobalCategoryName, _AverageResponseTimeCounterName, _ClientName);
            _PercentileResponseTime = _CounterRegistry.GetPercentileCounter(_GlobalCategoryName, "ResponseTime.Percentile.{0}", _Percentiles, _ClientName);
        }

        private PerInstancePerformanceMonitor GetOrCreateAction(string actionName)
        {
            return _ActionMonitors.GetOrAdd(actionName.IsNullOrEmpty() ? "(root)" : actionName, (result) => new PerInstancePerformanceMonitor(_CounterRegistry, _Category, actionName));
        }

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, int>> GetApplicationRequestRequestCountDictionaries()
        {
            var requestCache = RequestCacheDictionaryGetter?.Invoke();
            if (requestCache == null)
            {
                return null;
            }
            if (requestCache.Contains(_PerApplicationRequestRequestsCounterDictionaryKey))
            {
                return requestCache[_PerApplicationRequestRequestsCounterDictionaryKey] as ConcurrentDictionary<string, ConcurrentDictionary<string, int>>;
            }
            var perAppRequestCounterCache = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
            requestCache[_PerApplicationRequestRequestsCounterDictionaryKey] = perAppRequestCounterCache;
            return perAppRequestCounterCache;
        }

        private ConcurrentDictionary<string, int> GetApplicationRequestRequestCountDictionary()
        {
            return GetApplicationRequestRequestCountDictionaries()?.GetOrAdd(_ClientName, (clientName) => new ConcurrentDictionary<string, int>());
        }

        private const string _FailuresPerSecondCounterName = "Failures/s";

        private const string _SuccessesPerSecondCounterName = "Requests/s";

        private const string _AverageResponseTimeCounterName = "Average Response Time";

        private const string _TotalInstanceName = "_Total";

        private const string _PerApplicationRequestRequestsCounterDictionaryKey = "ClientRequestsMonitor:ApplicationRequest_TotalRequests";

        private static readonly byte[] _Percentiles = new byte[]
        {
            25,
            50,
            75,
            95,
            99
        };

        private static readonly ConcurrentDictionary<string, ClientRequestsMonitor> _ClientMonitors = new ConcurrentDictionary<string, ClientRequestsMonitor>();

        private readonly ICounterRegistry _CounterRegistry;

        private readonly ConcurrentDictionary<string, PerInstancePerformanceMonitor> _ActionMonitors = new ConcurrentDictionary<string, PerInstancePerformanceMonitor>();

        public static Func<IDictionary> RequestCacheDictionaryGetter = null;

        private readonly string _GlobalCategoryName;

        private readonly string _Category;

        private readonly string _ClientName;

        private IAverageValueCounter _AverageResponseTime;

        private IRateOfCountsPerSecondCounter _FailuresPerSecond;

        private IPercentileCounter _PercentileResponseTime;

        private IRateOfCountsPerSecondCounter _SuccessesPerSecond;
    }
}
