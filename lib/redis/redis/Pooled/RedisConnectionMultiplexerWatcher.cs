namespace Redis;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

internal class RedisConnectionMultiplexerWatcher
{
    private Dictionary<ConnectionType, CancellationTokenSource> _TokenSources = new();

    private readonly object _TokenSync = new();
    private readonly IConnectionBuilder _ConnectionBuilder;
    private readonly ConfigurationOptions _ConnectionConfiguration;
    private readonly RedisPooledClientOptions _ClientOptions;

    private WeakReference<IConnectionMultiplexer> _LastRefreshed;

    public IConnectionMultiplexer Connection { get; private set; }

    public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;

    public RedisConnectionMultiplexerWatcher(
        IConnectionMultiplexer connection, 
        IConnectionBuilder connectionBuilder,
        ConfigurationOptions connectionConfiguration,
        RedisPooledClientOptions clientOptions
    )
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _ConnectionBuilder = connectionBuilder ?? throw new ArgumentNullException(nameof(connectionBuilder));
        _ConnectionConfiguration = connectionConfiguration ?? throw new ArgumentNullException(nameof(connectionConfiguration));
        _ClientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));

        connection.ConnectionFailed += OnConnectionFailed;
        connection.ConnectionRestored += OnConnectionRestored;
    }

    public void Dispose()
    {
        lock (_TokenSync)
        {
            Connection.Dispose();

            foreach (var source in _TokenSources)
            {
                source.Value.Cancel();
                source.Value.Dispose();
            }

            _TokenSources.Clear();
        }
    }

    public void Close()
    {
        lock (_TokenSync)
        {
            Connection.Dispose();

            foreach (var source in _TokenSources)
            {
                source.Value.Cancel();
                source.Value.Dispose();
            }

            _TokenSources.Clear();
        }
    }

    private void OnConnectionRestored(object sender, ConnectionFailedEventArgs args)
    {
        if (_ClientOptions.MaxReconnectTimeout > 0)
        {
            lock (_TokenSync)
            {
                if (_LastRefreshed == null || !_LastRefreshed.TryGetTarget(out var mul) || sender != mul)
                {
                    if (_TokenSources.TryGetValue(args.ConnectionType, out var source))
                    {
                        source = _TokenSources[args.ConnectionType];
                        _TokenSources.Remove(args.ConnectionType);

                        source.Cancel();
                        source.Dispose();
                    }
                }
            }
        }
    }

    private void OnConnectionFailed(object sender, ConnectionFailedEventArgs args)
    {
        if (_ClientOptions.MaxReconnectTimeout > 0)
        {
            lock (_TokenSync)
            {
                if (_LastRefreshed != null && _LastRefreshed.TryGetTarget(out var mul) && sender == mul)
                    return;

                if (!_TokenSources.ContainsKey(args.ConnectionType))
                {
                    var tokenSource = new CancellationTokenSource();
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(_ClientOptions.MaxReconnectTimeout, tokenSource.Token).ConfigureAwait(false);
                            await RefreshConnection(Connection).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    });

                    _TokenSources.Add(args.ConnectionType, tokenSource);
                }
            }
        }

        ConnectionFailed?.Invoke(sender, args);
    }

    private async Task RefreshConnection(IConnectionMultiplexer oldConnection)
    {
        lock (_TokenSync)
        {
            if (Connection != oldConnection) return;

            _LastRefreshed = new WeakReference<IConnectionMultiplexer>(oldConnection);

            oldConnection.ConnectionFailed -= OnConnectionFailed;
            oldConnection.ConnectionRestored -= OnConnectionRestored;

            foreach (var source in _TokenSources)
            {
                source.Value.Cancel();
                source.Value.Dispose();
            }

            _TokenSources.Clear();
        }
        var mul = await _ConnectionBuilder.CreateConnectionMultiplexerAsync(_ConnectionConfiguration).ConfigureAwait(false);
        mul.ConnectionFailed += OnConnectionFailed;
        mul.ConnectionRestored += OnConnectionRestored;

        Connection = mul;

        oldConnection.Dispose();
    }
}
