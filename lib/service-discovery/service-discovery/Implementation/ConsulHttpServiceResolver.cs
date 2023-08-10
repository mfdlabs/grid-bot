namespace ServiceDiscovery;


using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Consul;

using Logging;
using Configuration;

using ISettings = global::ServiceDiscovery.Properties.ISettings;

/// <inheritdoc cref="IServiceResolver"/>
public class ConsulHttpServiceResolver : IServiceResolver, INotifyPropertyChanged, IDisposable
{
    private readonly ISettings _Settings;
    private readonly ILogger _Logger;
    private readonly ISingleSetting<string> _ServiceNameSetting;
    private readonly IConsulClientProvider _ConsulClientProvider;
    private readonly string _EnvironmentName;
    private readonly Thread _Thread;
    private CancellationTokenSource _CancellationTokenSource;

    /// <summary>
    /// Construct a new instance of <see cref="ConsulHttpServiceResolver"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="consulClientProvider">The <see cref="IConsulClientProvider"/></param>
    /// <param name="serviceNameSetting">The <see cref="ISingleSetting{T}"/></param>
    /// <param name="environmentName">The name of the environment.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="consulClientProvider"/> cannot be null.
    /// - <paramref name="serviceNameSetting"/> cannot be null.
    /// - <paramref name="environmentName"/> cannot be null.
    /// </exception>
    [ExcludeFromCodeCoverage]
    public ConsulHttpServiceResolver(
        ILogger logger,
        IConsulClientProvider consulClientProvider,
        ISingleSetting<string> serviceNameSetting,
        string environmentName
    ) : this(
            global::ServiceDiscovery.Properties.Settings.Default,
            logger,
            consulClientProvider,
            serviceNameSetting,
            environmentName
        )
    {
    }

    internal ConsulHttpServiceResolver(
        ISettings settings,
        ILogger logger,
        IConsulClientProvider consulClientProvider,
        ISingleSetting<string> serviceNameSetting,
        string environmentName
    )
    {
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ServiceNameSetting = serviceNameSetting ?? throw new ArgumentNullException(nameof(serviceNameSetting));
        _ConsulClientProvider = consulClientProvider ?? throw new ArgumentNullException(nameof(consulClientProvider));
        _EnvironmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));

        EndPoints = new HashSet<IPEndPoint>();

        _ServiceNameSetting.PropertyChanged += (sender, args) => _CancellationTokenSource?.Cancel();
        _ConsulClientProvider.PropertyChanged += (sender, args) => _CancellationTokenSource?.Cancel();

        _Thread = StartRefreshThread();
    }

    /// <inheritdoc cref="IServiceResolver.EndPoints"/>
    public ISet<IPEndPoint> EndPoints { get; private set; }

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        _Thread?.Abort();
    }

    private Thread StartRefreshThread()
    {
        var thread = new Thread(RefreshThread)
        {
            IsBackground = true
        };

        thread.Start();
        return thread;
    }

    private async void RefreshThread()
    {
        ulong? lastIndex = null;
        string lastServiceName = null;

        int failures = 0;

        while (true)
        {
            _CancellationTokenSource = new();

            try
            {
                if (_ServiceNameSetting.Value != lastServiceName)
                {
                    _Logger.Information(
                        "ConsulHttpServiceResolver: Service name changed from {0} to {1}.",
                        lastServiceName,
                        _ServiceNameSetting.Value
                    );

                    lastServiceName = _ServiceNameSetting.Value;
                    lastIndex = null;
                }

                lastIndex = await DoRefreshAsync(lastServiceName, lastIndex, _CancellationTokenSource.Token).ConfigureAwait(false);

                failures = 0;
            }
            catch (TaskCanceledException)
            {
                _Logger.Debug("ConsulHttpServiceResolver: TaskCanceledException in resolving thread.");
            }
            catch (Exception ex)
            {
                _Logger.Error(
                    "ConsulHttpServiceResolver: Exception while resolving {0}. There have been {1} continuous failures. {2}",
                    lastServiceName,
                    failures,
                    ex
                );

                lastIndex = null;
                await Task.Delay(DetermineBackoffDelayTime(failures, _Settings.ConsulBackoffBase, _Settings.MaximumConsulBackoff)).ConfigureAwait(false);
                failures++;
            }
        }
    }

    private async Task<ulong?> DoRefreshAsync(string serviceName, ulong? lastIndex, CancellationToken cancellationToken)
    {
        var queryOptions = new QueryOptions();

        if (lastIndex != null)
        {
            queryOptions.WaitTime = _Settings.ConsulLongPollingMaxWaitTime;
            queryOptions.WaitIndex = lastIndex.Value;
        }

        var queryResult = await _ConsulClientProvider.Client.Health.Service(
            serviceName,
            _EnvironmentName,
            true,
            queryOptions,
            cancellationToken
        ).ConfigureAwait(false);

        if (queryResult.StatusCode != HttpStatusCode.OK)
            throw new Exception(string.Format("StatusCode while retrieving service endpoints: {0}", queryResult.StatusCode));

        cancellationToken.ThrowIfCancellationRequested();

        var endpoints = ParseCatalogServiceResults(queryResult.Response);

        _Logger.Information(
            UpdateEndpointsIfChanged(endpoints)
               ? () => string.Format("Fetched new endpoints for {0}: {1}", serviceName, string.Join(", ", endpoints))
               : () => string.Format("Endpoints for {0} have not changed.", serviceName)
        );

        return queryResult.LastIndex;
    }

    private IEnumerable<IPEndPoint> ParseCatalogServiceResults(IEnumerable<ServiceEntry> catalogServices)
    {
        return from s in catalogServices
               select new IPEndPoint(IPAddress.Parse(s.Node.Address), s.Service.Port);
    }

    private TimeSpan DetermineBackoffDelayTime(int failures, TimeSpan backoffBase, TimeSpan maxBackoff) 
        => TimeSpan.FromTicks(Math.Min(backoffBase.Ticks * failures, maxBackoff.Ticks));

    private bool UpdateEndpointsIfChanged(IEnumerable<IPEndPoint> newEndPoints)
    {
        var endpoints = new HashSet<IPEndPoint>(newEndPoints);
        if (EndPoints.SetEquals(endpoints))
            return false;

        EndPoints = endpoints;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndPoints)));
        return true;
    }
}
