namespace Redis;

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

public partial class SwitchingConnectionBuilder
{
    private class SwitchingConnectionMultiplexer : IConnectionMultiplexer
    {
        private bool _UseSecond;
        private IConnectionMultiplexer _CurrentConnectionMultiplexer;

        private readonly IConnectionBuilder _FirstConnectionBuilder;
        private readonly IConnectionBuilder _SecondConnectionBuilder;
        private readonly ConfigurationOptions _Configuration;

        public string ClientName => _CurrentConnectionMultiplexer.ClientName;
        public string Configuration => _CurrentConnectionMultiplexer.Configuration;
        public int TimeoutMilliseconds => _CurrentConnectionMultiplexer.TimeoutMilliseconds;
        public long OperationCount => _CurrentConnectionMultiplexer.OperationCount;
        public bool PreserveAsyncOrder
        {
            get => _CurrentConnectionMultiplexer.PreserveAsyncOrder;
            set => _CurrentConnectionMultiplexer.PreserveAsyncOrder = value;
        }
        public bool IsConnected => _CurrentConnectionMultiplexer.IsConnected;
        public bool IncludeDetailInExceptions
        {
            get => _CurrentConnectionMultiplexer.IncludeDetailInExceptions;
            set => _CurrentConnectionMultiplexer.IncludeDetailInExceptions = value;
        }
        public int StormLogThreshold
        {
            get => _CurrentConnectionMultiplexer.StormLogThreshold;
            set => _CurrentConnectionMultiplexer.StormLogThreshold = value;
        }

        public event EventHandler<RedisErrorEventArgs> ErrorMessage;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;
        public event EventHandler<InternalErrorEventArgs> InternalError;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored;
        public event EventHandler<EndPointEventArgs> ConfigurationChanged;
        public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast;
        public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved;

        public SwitchingConnectionMultiplexer(IConnectionMultiplexer initialConnectionMultiplexer, IConnectionBuilder firstConnectionMultiplexer, IConnectionBuilder secondConnectionBuilder, bool initialUseSecond, ConfigurationOptions configuration)
        {
            _CurrentConnectionMultiplexer = initialConnectionMultiplexer;
            _FirstConnectionBuilder = firstConnectionMultiplexer;
            _SecondConnectionBuilder = secondConnectionBuilder;
            _UseSecond = initialUseSecond;
            _Configuration = configuration;

            SubscribeToEvents(_CurrentConnectionMultiplexer);
        }

        public async void SetSwitch(bool useSecond)
        {
            if (_UseSecond == useSecond) return;

            _UseSecond = true;

            var connectionMultiplexer = await (_UseSecond 
                ? _SecondConnectionBuilder 
                : _FirstConnectionBuilder).CreateConnectionMultiplexerAsync(_Configuration).ConfigureAwait(false);

            SubscribeToEvents(connectionMultiplexer);

            var oldConnectionMultiplexer = Interlocked.Exchange(ref _CurrentConnectionMultiplexer, connectionMultiplexer);
            
            await oldConnectionMultiplexer.CloseAsync(true).ConfigureAwait(false);

            UnsubscribeFromEvents(oldConnectionMultiplexer);
        }

        public void RegisterProfiler(IProfiler profiler) 
            => _CurrentConnectionMultiplexer.RegisterProfiler(profiler);

        public void BeginProfiling(object forContext) 
            => _CurrentConnectionMultiplexer.BeginProfiling(forContext);

        public ProfiledCommandEnumerable FinishProfiling(object forContext, bool allowCleanupSweep = true) 
            => _CurrentConnectionMultiplexer.FinishProfiling(forContext, allowCleanupSweep);

        public ServerCounters GetCounters() 
            => _CurrentConnectionMultiplexer.GetCounters();

        public EndPoint[] GetEndPoints(bool configuredOnly = false) 
            => _CurrentConnectionMultiplexer.GetEndPoints(configuredOnly);

        public void Wait(Task task) 
            => _CurrentConnectionMultiplexer.Wait(task);

        public T Wait<T>(Task<T> task) 
            => _CurrentConnectionMultiplexer.Wait<T>(task);

        public void WaitAll(params Task[] tasks)
            => _CurrentConnectionMultiplexer.WaitAll(tasks);

        public int HashSlot(RedisKey key) 
            => _CurrentConnectionMultiplexer.HashSlot(key);

        public ISubscriber GetSubscriber(object asyncState = null) 
            => _CurrentConnectionMultiplexer.GetSubscriber(asyncState);

        public IDatabase GetDatabase(int db = -1, object asyncState = null) 
            => _CurrentConnectionMultiplexer.GetDatabase(db, asyncState);

        public IServer GetServer(string host, int port, object asyncState = null) 
            => _CurrentConnectionMultiplexer.GetServer(host, port, asyncState);

        public IServer GetServer(string hostAndPort, object asyncState = null)
            => _CurrentConnectionMultiplexer.GetServer(hostAndPort, asyncState);

        public IServer GetServer(IPAddress host, int port) 
            => _CurrentConnectionMultiplexer.GetServer(host, port);

        public IServer GetServer(EndPoint endpoint, object asyncState = null) 
            => _CurrentConnectionMultiplexer.GetServer(endpoint, asyncState);

        public Task<bool> ConfigureAsync(TextWriter log = null) 
            => _CurrentConnectionMultiplexer.ConfigureAsync(log);

        public bool Configure(TextWriter log = null) 
            => _CurrentConnectionMultiplexer.Configure(log);

        public string GetStatus() 
            => _CurrentConnectionMultiplexer.GetStatus();

        public void GetStatus(TextWriter log) 
            => _CurrentConnectionMultiplexer.GetStatus(log);

        public void Close(bool allowCommandsToComplete = true) 
            => _CurrentConnectionMultiplexer.Close(allowCommandsToComplete);

        public Task CloseAsync(bool allowCommandsToComplete = true) 
            => _CurrentConnectionMultiplexer.CloseAsync(allowCommandsToComplete);

        public void Dispose() => _CurrentConnectionMultiplexer.Dispose();

        public string GetStormLog()
            => _CurrentConnectionMultiplexer.GetStormLog();

        public void ResetStormLog() 
            => _CurrentConnectionMultiplexer.ResetStormLog();

        public long PublishReconfigure(CommandFlags flags = CommandFlags.None) 
            => _CurrentConnectionMultiplexer.PublishReconfigure(flags);

        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None) 
            => _CurrentConnectionMultiplexer.PublishReconfigureAsync(flags);

        private void SubscribeToEvents(IConnectionMultiplexer cm)
        {
            cm.ErrorMessage += OnErrorMessage;
            cm.ConfigurationChanged += OnConfigurationChanged;
            cm.ConfigurationChangedBroadcast += OnConfigurationChangedBroadcast;
            cm.ConnectionFailed += OnConnectionFailed;
            cm.ConnectionRestored += OnConnectionRestored;
            cm.HashSlotMoved += OnHashSlotMoved;
            cm.InternalError += OnInternalError;
        }

        private void UnsubscribeFromEvents(IConnectionMultiplexer cm)
        {
            cm.ErrorMessage -= OnErrorMessage;
            cm.ConfigurationChanged -= OnConfigurationChanged;
            cm.ConfigurationChangedBroadcast -= OnConfigurationChangedBroadcast;
            cm.ConnectionFailed -= OnConnectionFailed;
            cm.ConnectionRestored -= OnConnectionRestored;
            cm.HashSlotMoved -= OnHashSlotMoved;
            cm.InternalError -= OnInternalError;
        }

        private void OnHashSlotMoved(object sender, HashSlotMovedEventArgs e)
            => HashSlotMoved?.Invoke(sender, e);

        private void OnConfigurationChangedBroadcast(object sender, EndPointEventArgs e)
            => ConfigurationChangedBroadcast?.Invoke(sender, e);

        private void OnConfigurationChanged(object sender, EndPointEventArgs e)
            => ConfigurationChanged?.Invoke(sender, e);

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
            => ConnectionRestored?.Invoke(sender, e);

        private void OnInternalError(object sender, InternalErrorEventArgs e)
            => InternalError?.Invoke(sender, e);

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
            => ConnectionFailed?.Invoke(sender, e);

        private void OnErrorMessage(object sender, RedisErrorEventArgs e)
            => ErrorMessage?.Invoke(sender, e);
    }
}
