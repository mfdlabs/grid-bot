using MFDLabs.Logging;
using Microsoft.Ccr.Core;
using System;
using System.Threading;

namespace MFDLabs.Concurrency.Base
{
    /// <summary>
    /// WORK IN PROGRESS DO NOT USE IN PROD!!!
    /// </summary>
    /// <typeparam name="TSingleton"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public abstract class ExpiringTaskThread<TSingleton, TItem> : BaseTaskThread<TSingleton, TItem>, IDisposable
        where TSingleton : ExpiringTaskThread<TSingleton, TItem>, IDisposable, new()
        where TItem : class
    {
        #region Overloaded Members

        /// <inheritdoc/>
        public static new TSingleton Singleton
        {
            get
            {
                if (_singleton == null) _singleton = new TSingleton();
                return _singleton;
            }
        }

        #endregion Overloaded Members

        #region Members

        /// <summary>
        /// The timeout to be implemented when this <see cref="ExpiringTaskThread{TSingleton, TItem}"/> to expire.
        /// </summary>
        public abstract TimeSpan Expiration { get; }

        #endregion Members

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();
            _reloadTimer = new Timer((o) => DetermineIfDeletionNeeded(), null, Expiration, Expiration);
        }

        /// <inheritdoc/>
        protected override void ThreadWorker()
        {
            SystemLogger.Singleton.Debug("Starting expiring '{0}' with the delay of '{1}' and expiration of '{2}'", Name, ProcessActivationInterval, Expiration);

            while (true)
            {
                if (Port.ItemCount > 0)
                {
                    lock (_lock)
                    {
                        if (CanReceive)
                        {
                            ConcurrencyService.Singleton.Activate(
                                Arbiter.Receive(
                                    false,
                                    Port,
                                    (item) =>
                                    {
                                        _sequenceID++;
                                        _monitor.CountOfItemsProcessed.Increment();
                                        _monitor.RateOfItemsPerSecondProcessed.Increment();
                                        _monitor.AverageRateOfItems.Sample(1.0 / _sequenceID);
                                        _isProcessingItem = true;
                                        lock (_resultLock)
                                        {
                                            try
                                            {
                                                var packet = new Packet<TItem>(item, PacketID, _sequenceID, _monitor);
                                                _lastResult = OnReceive(ref packet);
                                                if (packet.Status == PacketProcessingStatus.Failure)
                                                {
                                                    _monitor.CountOfItemsProcessedThatFail.Increment();
                                                    _monitor.RateOfItemsPerSecondProcessedThatFail.Increment();
                                                    _monitor.AverageRateOfItemsThatFail.Sample(1.0 / _sequenceID);
                                                }
                                                else
                                                {
                                                    _monitor.CountOfItemsProcessedThatSucceed.Increment();
                                                    _monitor.RateOfItemsPerSecondProcessedThatSucceed.Increment();
                                                    _monitor.AverageRateOfItemsThatSucceed.Sample(1.0 / _sequenceID);
                                                }
                                                packet.Dispose();
                                            }
                                            catch (Exception ex)
                                            {
                                                _monitor.CountOfItemsProcessedThatFail.Increment();
                                                _monitor.RateOfItemsPerSecondProcessedThatFail.Increment();
                                                _monitor.AverageRateOfItemsThatFail.Sample(1.0 / _sequenceID);

#if DEBUG
                                            SystemLogger.Singleton.Error(ex);
#else
                                            SystemLogger.Singleton.Warning("An error occurred when trying to process a received task item: {0}", ex.Message);
#endif
                                        }
                                        }
                                        _isProcessingItem = false;
                                        _lastUsedTimeUtc = DateTime.UtcNow;
                                    }
                                )
                            );
                        }
                        lock (_resultLock)
                            if (_lastResult == PluginResult.StopProcessingAndDeallocate)
                            {
                                _lastUsedTimeUtc = DateTime.MinValue;
                                _isProcessingItem = false;
                                CanReceive = false;
                                DetermineIfDeletionNeeded();
                                break;
                            }
                    }
                }

                Thread.Sleep(ProcessActivationInterval);
            }
        }

        private static void DetermineIfDeletionNeeded()
        {
#if DEBUG
            SystemLogger.Singleton.LifecycleEvent("Determining if task thread '{0}' has expired.", _singleton == null ? "Expired Task Thread" : _singleton.Name);
#endif
            if (_singleton != null)
                lock (_lock)
                {
                    if (_singleton._lastUsedTimeUtc < DateTime.UtcNow.Subtract(_singleton.Expiration) && _singleton.Port.ItemCount == 0 && !_singleton._isProcessingItem)
                    {
                        SystemLogger.Singleton.LifecycleEvent("Task thread '{0}' has expired, please re-index Singleton to reinstantiate it.", _singleton.Name);
                        _singleton.Dispose();
                        _singleton = null;
                        return;
                    }
                }
#if DEBUG
            SystemLogger.Singleton.Verbose("Task thread '{0}' has not expired.", _singleton == null ? "Expired Task Thread" : _singleton.Name);
#endif
            _singleton._reloadTimer.Change(_singleton.Expiration, _singleton.Expiration);
        }

        #region IDisposable Members

        /// <inheritdoc/>
        public void Dispose()
        {
            _reloadTimer?.Dispose();
        }

        #endregion IDisposable Members

        #region Concurrency

        private static readonly object _lock = new object();
        private readonly object _resultLock = new object();
        private bool _isProcessingItem = false;

        #endregion Concurrency

        #region Expiration Helpers

        private Timer _reloadTimer;
        private DateTime _lastUsedTimeUtc = DateTime.UtcNow;

        #endregion Expiration Helpers

        #region Other Items

        private PluginResult _lastResult;
        private int _sequenceID = 0;
        private static TSingleton _singleton;

        #endregion Other Items
    }
}
