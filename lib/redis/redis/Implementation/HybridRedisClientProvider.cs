namespace Redis;

using System;
using System.Net;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

using Logging;
using Configuration;
using ServiceDiscovery;

/// <summary>
/// Redis Client provider to dynamically fetch a client with or without endpoints.
/// </summary>
public sealed class HybridRedisClientProvider : IRedisClientProvider
{
    private readonly ILogger _Logger;
    private readonly IServiceResolver _ServiceResolver;
    private readonly RedisClientOptions _ClientOptions;
    private readonly ISingleSetting<bool> _UseServiceDiscovery;
    private readonly IHybridRedisClientProviderSettings _Settings;
    private readonly ISingleSetting<RedisEndpoints> _RedisEndpoints;
    private readonly object _Lock = new();
    private readonly TaskCompletionSource<RedisEndpoints> _ResolverEndpoints = new();

    private RedisClient _RedisClient;
    private RedisEndpoints _CurrentEndpoints;
    private bool _Disposed;

    /// <inheritdoc cref="IRedisClientProvider.Client"/>
    public IRedisClient Client
    {
        get
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(HybridRedisClientProvider));

            if (_RedisClient == null)
                lock (_Lock)
                    _RedisClient ??= CreateRedisClient();

            return _RedisClient;
        }
    }

    /// <summary>
    /// Construct a new instance of <see cref="HybridRedisClientProvider"/>
    /// </summary>
    /// <param name="settings">The <see cref="IHybridRedisClientProviderSettings"/></param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="serviceResolver">The <see cref="IServiceResolver"/></param>
    /// <param name="useServiceDiscovery">Should use service discovery.</param>
    /// <param name="redisEndpoints">The inital redis endpoints.</param>
    /// <param name="clientOptions">The client options.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="settings"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="serviceResolver"/> cannot be null.
    /// - <paramref name="useServiceDiscovery"/> cannot be null.
    /// - <paramref name="redisEndpoints"/> cannot be null.
    /// </exception>
    public HybridRedisClientProvider(
        IHybridRedisClientProviderSettings settings,
        ILogger logger, 
        IServiceResolver serviceResolver, 
        ISingleSetting<bool> useServiceDiscovery,
        ISingleSetting<RedisEndpoints> redisEndpoints,
        RedisClientOptions clientOptions = null
    )
    {
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ServiceResolver = serviceResolver ?? throw new ArgumentNullException(nameof(serviceResolver));
        _UseServiceDiscovery = useServiceDiscovery ?? throw new ArgumentNullException(nameof(useServiceDiscovery));
        _RedisEndpoints = redisEndpoints ?? throw new ArgumentNullException(nameof(redisEndpoints));

        _ClientOptions = clientOptions;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        _Disposed = true;
        _ServiceResolver.PropertyChanged -= OnResolverChange;
        _UseServiceDiscovery.PropertyChanged -= OnSettingsChange;
        _RedisEndpoints.PropertyChanged -= OnSettingsChange;

        lock (_Lock)
            _RedisClient?.Close();
    }

    private RedisClient CreateRedisClient()
    {
        _ServiceResolver.PropertyChanged += OnResolverChange;
        _UseServiceDiscovery.PropertyChanged += OnSettingsChange;
        _RedisEndpoints.PropertyChanged += OnSettingsChange;

        if (_UseServiceDiscovery.Value)
        {
            var initialDiscoveryWaitTime = _Settings.InitialDiscoveryWaitTime;

            if (!_ResolverEndpoints.Task.Wait(initialDiscoveryWaitTime))
            {
                _Logger.Error("Could not obtain the initial Redis endpoints from the service resolver within {0:N0} seconds", initialDiscoveryWaitTime.TotalSeconds);
                _CurrentEndpoints = null;
            }
            else
                _CurrentEndpoints = _ResolverEndpoints.Task.Result;

        }
        else
            _CurrentEndpoints = _RedisEndpoints.Value;

        RedisClient client;
        if (_CurrentEndpoints != null)
        {
            client = new RedisClient(_CurrentEndpoints, _Logger.Error, _ClientOptions);
            _Logger.Information("Created Redis client with endpoints: {0}", _CurrentEndpoints);
        }
        else
        {
            client = new RedisClient(Array.Empty<string>(), _Logger.Error, _ClientOptions);
            _Logger.Warning("Created Redis client with no endpoints");
        }

        return client;
    }

    private void OnResolverChange(object sender, PropertyChangedEventArgs eventArgs)
    {
        var redisEndpoints = ToRedisEndpoints(_ServiceResolver.EndPoints);
        if (redisEndpoints != null)
            _ResolverEndpoints.TrySetResult(redisEndpoints);

        lock (_Lock)
            if (_RedisClient != null)
                RefreshRedisClient(_RedisClient);
    }

    private void OnSettingsChange(object sender, PropertyChangedEventArgs eventArgs)
    {
        lock (_Lock)
            if (_RedisClient != null)
                RefreshRedisClient(_RedisClient);
    }

    private void RefreshRedisClient(RedisClient client)
    {
        var newEndpoints = _UseServiceDiscovery.Value 
            ? ToRedisEndpoints(_ServiceResolver.EndPoints) 
            : _RedisEndpoints.Value;

        if (newEndpoints == null)
        {
            _Logger.Warning("Ignoring empty Redis endpoint list. Current endpoints: {0}", _CurrentEndpoints);
            return;
        }

        if (_CurrentEndpoints != null && _CurrentEndpoints.HasTheSameEndpoints(newEndpoints))
            return;

        client.Refresh(newEndpoints);

        _Logger.Information("Refreshed Redis endpoints. New endpoints: {0}", newEndpoints);

        _CurrentEndpoints = newEndpoints;
    }

    private static RedisEndpoints ToRedisEndpoints(IEnumerable<IPEndPoint> endpoints)
    {
        if (!endpoints.Any()) return null;

        return new RedisEndpoints(string.Join(",", endpoints));
    }
}
