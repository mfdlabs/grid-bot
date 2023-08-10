namespace Redis;

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

public partial class SelfHealingConnectionBuilder
{
    private partial class SelfHealingConnectionMultiplexer : IConnectionMultiplexer
    {
        private IConnectionMultiplexer _Decorated;

        private readonly IConnectionBuilder _ConnectionBuilder;
        private readonly ConfigurationOptions _ConfigurationOptions;
        private readonly ReaderWriterLockSlim _ReaderWriterLockSlim;
        private readonly BadStateRecorder _BadStateRecorder;

        private volatile bool _IsDisposed;

        public SelfHealingConnectionMultiplexer(IConnectionMultiplexer initialConnectionMultiplexer, IConnectionBuilder connectionBuilder, ConfigurationOptions configurationOptions, ISelfHealingConnectionMultiplexerSettings settings, Func<DateTime> getCurrentTime)
        {
            _Decorated = initialConnectionMultiplexer ?? throw new ArgumentNullException(nameof(initialConnectionMultiplexer));
            _ConnectionBuilder = connectionBuilder ?? throw new ArgumentNullException(nameof(connectionBuilder));
            _ConfigurationOptions = configurationOptions ?? throw new ArgumentNullException(nameof(configurationOptions));

            _ReaderWriterLockSlim = new();

            _BadStateRecorder = new(settings, getCurrentTime);
            _BadStateRecorder.BadStateDetected += OnBadStateDetected;
        }

        ~SelfHealingConnectionMultiplexer()
        {
            Dispose(false);
        }

        public string ClientName => _Decorated.ClientName;
        public string Configuration => _Decorated.Configuration;
        public int TimeoutMilliseconds => _Decorated.TimeoutMilliseconds;
        public long OperationCount => _Decorated.OperationCount;
        public bool PreserveAsyncOrder
        {
            get => _Decorated.PreserveAsyncOrder;
            set => _Decorated.PreserveAsyncOrder = value;
        }
        public bool IsConnected => _Decorated.IsConnected;
        public bool IncludeDetailInExceptions
        {
            get => _Decorated.IncludeDetailInExceptions;
            set => _Decorated.IncludeDetailInExceptions = value;
        }
        public int StormLogThreshold
        {
            get => _Decorated.StormLogThreshold;
            set => _Decorated.StormLogThreshold = value;
        }

        public event EventHandler<RedisErrorEventArgs> ErrorMessage;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;
        public event EventHandler<InternalErrorEventArgs> InternalError;
        public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored;
        public event EventHandler<EndPointEventArgs> ConfigurationChanged;
        public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast;
        public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved;

        public void BeginProfiling(object forContext)
            => DoDecoratedOperation(cm => cm.BeginProfiling(forContext));

        public void Close(bool allowCommandsToComplete = true)
            => DoDecoratedOperation(cm => cm.Close(allowCommandsToComplete));

        public Task CloseAsync(bool allowCommandsToComplete = true)
            => DoDecoratedOperation(cm => cm.CloseAsync(allowCommandsToComplete));

        public bool Configure(TextWriter log = null)
            => DoDecoratedOperation(cm => cm.Configure(log));

        public Task<bool> ConfigureAsync(TextWriter log = null)
            => DoDecoratedOperation(cm => cm.ConfigureAsync(log));

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (_IsDisposed) return;

            if (isDisposing)
            {
                _ReaderWriterLockSlim.Dispose();

                _BadStateRecorder.BadStateDetected -= OnBadStateDetected;

                _Decorated.ConfigurationChangedBroadcast -= OnConfigurationChangedBroadcast;
                _Decorated.ConfigurationChanged -= OnConfigurationChanged;
                _Decorated.ConnectionFailed -= OnConnectionFailed;
                _Decorated.ConnectionRestored -= OnConnectionRestored;
                _Decorated.ErrorMessage -= OnErrorMessage;
                _Decorated.HashSlotMoved -= OnHashSlotMoved;
                _Decorated.InternalError -= OnInternalError;
                _Decorated.Dispose();
            }

            _IsDisposed = true;
        }

        public ProfiledCommandEnumerable FinishProfiling(object forContext, bool allowCleanupSweep = true)
            => DoDecoratedOperation(cm => cm.FinishProfiling(forContext, allowCleanupSweep));

        public ServerCounters GetCounters()
            => DoDecoratedOperation(cm => cm.GetCounters());

        public IDatabase GetDatabase(int db = -1, object asyncState = null)
        {
            var database = new BadStateReportingDatabase(DoDecoratedOperation(cm => cm.GetDatabase(db, asyncState)));
            database.BadStateExceptionOccurred += _BadStateRecorder.Record;

            return database;
        }

        public EndPoint[] GetEndPoints(bool configuredOnly = false)
            => DoDecoratedOperation(cm => cm.GetEndPoints(configuredOnly));

        public IServer GetServer(string host, int port, object asyncState = null)
        {
            var server = new BadStateReportingServer(DoDecoratedOperation(cm => cm.GetServer(host, port, asyncState)));
            server.BadStateExceptionOccurred += _BadStateRecorder.Record;

            return server;
        }

        public IServer GetServer(string hostAndPort, object asyncState = null)
        {
            var server = new BadStateReportingServer(DoDecoratedOperation(cm => cm.GetServer(hostAndPort, asyncState)));
            server.BadStateExceptionOccurred += _BadStateRecorder.Record;

            return server;
        }

        public IServer GetServer(IPAddress host, int port)
        {
            var server = new BadStateReportingServer(DoDecoratedOperation(cm => cm.GetServer(host, port)));
            server.BadStateExceptionOccurred += _BadStateRecorder.Record;

            return server;
        }

        public IServer GetServer(EndPoint endpoint, object asyncState = null)
        {
            var server = new BadStateReportingServer(DoDecoratedOperation(cm => cm.GetServer(endpoint, asyncState)));
            server.BadStateExceptionOccurred += _BadStateRecorder.Record;

            return server;
        }

        public string GetStatus()
            => DoDecoratedOperation(cm => cm.GetStatus());

        public void GetStatus(TextWriter log)
            => DoDecoratedOperation(cm => cm.GetStatus(log));

        public string GetStormLog()
            => DoDecoratedOperation(cm => cm.GetStormLog());

        public ISubscriber GetSubscriber(object asyncState = null)
        {
            var subscriber = new BadStateReportingSubscriber(DoDecoratedOperation(cm => cm.GetSubscriber(asyncState)));
            subscriber.BadStateExceptionOccurred += _BadStateRecorder.Record;

            return subscriber;
        }

        public int HashSlot(RedisKey key)
            => DoDecoratedOperation(cm => cm.HashSlot(key));

        public long PublishReconfigure(CommandFlags flags = CommandFlags.None)
            => DoDecoratedOperation(cm => cm.PublishReconfigure(flags));

        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None)
            => DoDecoratedOperation(cm => cm.PublishReconfigureAsync(flags));

        public void RegisterProfiler(IProfiler profiler)
            => DoDecoratedOperation(cm => cm.RegisterProfiler(profiler));

        public void ResetStormLog()
            => DoDecoratedOperation(cm => cm.ResetStormLog());

        public void Wait(Task task)
            => DoDecoratedOperation(cm => cm.Wait(task));

        public T Wait<T>(Task<T> task)
            => DoDecoratedOperation(cm => cm.Wait(task));

        public void WaitAll(params Task[] tasks)
            => DoDecoratedOperation(cm => cm.WaitAll(tasks));

        private void OnBadStateDetected(long recorderVersion)
        {
            if (_BadStateRecorder.Version != recorderVersion) return;

            _ReaderWriterLockSlim.TryEnterWriteLock(-1);
            try
            {
                IConnectionMultiplexer connectionMultiplexer = null;
                if (_BadStateRecorder.Reset(recorderVersion))
                {
                    var result = Task.Run(() => _ConnectionBuilder.CreateConnectionMultiplexerAsync(_ConfigurationOptions)).GetAwaiter().GetResult();

                    _Decorated.ErrorMessage -= OnErrorMessage;
                    result.ErrorMessage += OnErrorMessage;

                    _Decorated.ConnectionFailed -= OnConnectionFailed;
                    result.ConnectionFailed += OnConnectionFailed;

                    _Decorated.InternalError -= OnInternalError;
                    result.InternalError += OnInternalError;

                    _Decorated.ConnectionRestored -= OnConnectionRestored;
                    result.ConnectionRestored += OnConnectionRestored;

                    _Decorated.ConfigurationChanged -= OnConfigurationChanged;
                    result.ConfigurationChanged += OnConfigurationChanged;

                    _Decorated.ConfigurationChangedBroadcast -= OnConfigurationChangedBroadcast;
                    result.ConfigurationChangedBroadcast += OnConfigurationChangedBroadcast;

                    _Decorated.HashSlotMoved -= OnHashSlotMoved;
                    result.HashSlotMoved += OnHashSlotMoved;

                    connectionMultiplexer = Interlocked.Exchange(ref _Decorated, result);
                }

                connectionMultiplexer?.Dispose();
            }
            finally
            {
                _ReaderWriterLockSlim.ExitWriteLock();
            }
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

        private T DoDecoratedOperation<T>(Func<IConnectionMultiplexer, T> operation)
        {
            if (_IsDisposed) throw new ObjectDisposedException(nameof(SelfHealingConnectionMultiplexer));

            try
            {
                _ReaderWriterLockSlim.TryEnterReadLock(-1);
                var result = operation(_Decorated);
                _ReaderWriterLockSlim.ExitReadLock();
                return result;
            }
            catch (RedisConnectionException)
            {
                _ReaderWriterLockSlim.ExitReadLock();
                _BadStateRecorder.Record();
                throw;
            }
            catch (RedisTimeoutException)
            {
                _ReaderWriterLockSlim.ExitReadLock();
                _BadStateRecorder.Record();
                throw;
            }
        }

        private void DoDecoratedOperation(Action<IConnectionMultiplexer> operation)
        {
            if (_IsDisposed) throw new ObjectDisposedException(nameof(SelfHealingConnectionMultiplexer));

            try
            {
                _ReaderWriterLockSlim.TryEnterReadLock(-1);
                operation(_Decorated);
                _ReaderWriterLockSlim.ExitReadLock();
            }
            catch (RedisConnectionException)
            {
                _ReaderWriterLockSlim.ExitReadLock();
                _BadStateRecorder.Record();
                throw;
            }
            catch (RedisTimeoutException)
            {
                _ReaderWriterLockSlim.ExitReadLock();
                _BadStateRecorder.Record();
                throw;
            }
        }
    }
}
