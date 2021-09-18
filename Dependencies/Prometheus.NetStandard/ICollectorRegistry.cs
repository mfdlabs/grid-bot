﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus
{
    /// <summary>
    /// Allows for substitution of CollectorRegistry in tests.
    /// Not used by prometheus-net itself - you cannot provide your own implementation to prometheus-net code, only to your own code.
    /// </summary>
    public interface ICollectorRegistry
    {
        void AddBeforeCollectCallback(Action callback);
        void AddBeforeCollectCallback(Func<CancellationToken, Task> callback);

        IEnumerable<KeyValuePair<string, string>> StaticLabels { get; }
        void SetStaticLabels(Dictionary<string, string> labels);

        Task CollectAndExportAsTextAsync(Stream to, CancellationToken cancel = default);
    }
}
