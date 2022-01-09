using System;
using System.Threading;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Logging;
using Microsoft.Ccr.Core;

namespace MFDLabs.Concurrency.Base.Unsafe
{
    /// <summary>
    /// WORK IN PROGRESS DO NOT USE IN PROD!!!
    /// </summary>
    /// <typeparam name="TSingleton"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public abstract class UnsafeExpiringTaskThread<TSingleton, TItem> : UnsafeBaseTaskThread<TSingleton, TItem>, IDisposable
        where TSingleton : UnsafeExpiringTaskThread<TSingleton, TItem>, IDisposable, new()
        where TItem : class
    {
        #region Overloaded Members

        /// <summary>
        /// </summary>
        public new static TSingleton Singleton => _singleton ?? (_singleton = new TSingleton());

        #endregion Overloaded Members

        #region Members

        /// <summary>
        /// The timeout to be implemented when this <see cref="UnsafeExpiringTaskThread{TSingleton, TItem}"/> to expire.
        /// </summary>
        protected abstract TimeSpan Expiration { get; }

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
            SystemLogger.Singleton.Debug("Starting expiring '{0}' with the delay of '{1}' and expiration of '{2}'",
                Name,
                ProcessActivationInterval,
                Expiration);

            while (CanReceive)
            {
                if (Port.ItemCount > 0)
                {
                    ConcurrencyService.Singleton.Activate(
                        Arbiter.Receive(
                            true,
                            Port,
                            (item) =>
                            {
                                _sequenceId++;
                                Monitor.CountOfItemsProcessed.Increment();
                                Monitor.RateOfItemsPerSecondProcessed.Increment();
                                Monitor.AverageRateOfItems.Sample(1.0 / _sequenceId);
                                _isProcessingItem = true;
                                try
                                {
                                    unsafe
                                    {
                                        var packet = new MFDLabs.Concurrency.Unsafe.Packet
                                        {
                                            Id = PacketId,
                                            SequenceId = _sequenceId,
                                            Data = GetRawDataBuffer(item),
                                            Created = DateTime.Now,
                                            Status = PacketProcessingStatus.Success
                                        };

                                        var pkt = packet.ToPtr();

                                        _lastResult = OnReceive(pkt);
                                        if (packet.Status == PacketProcessingStatus.Failure)
                                        {
                                            Monitor.CountOfItemsProcessedThatFail.Increment();
                                            Monitor.RateOfItemsPerSecondProcessedThatFail.Increment();
                                            Monitor.AverageRateOfItemsThatFail.Sample(1.0 / _sequenceId);
                                        }
                                        else
                                        {
                                            Monitor.CountOfItemsProcessedThatSucceed.Increment();
                                            Monitor.RateOfItemsPerSecondProcessedThatSucceed.Increment();
                                            Monitor.AverageRateOfItemsThatSucceed.Sample(1.0 / _sequenceId);
                                        }
                                        
                                        if (_lastResult == PluginResult.StopProcessingAndDeallocate)
                                        {
                                            _lastUsedTimeUtc = DateTime.MinValue;
                                            _isProcessingItem = false;
                                            CanReceive = false;
                                            DetermineIfDeletionNeeded();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Monitor.CountOfItemsProcessedThatFail.Increment();
                                    Monitor.RateOfItemsPerSecondProcessedThatFail.Increment();
                                    Monitor.AverageRateOfItemsThatFail.Sample(1.0 / _sequenceId);

#if DEBUG
                                    SystemLogger.Singleton.Error(ex);
#else
                                    SystemLogger.Singleton.Warning("An error occurred when trying to process a received task item: {0}", ex.Message);
#endif
                                }
                                _isProcessingItem = false;
                                _lastUsedTimeUtc = DateTime.UtcNow;
                            }
                        )
                    );
                            
                }

                Thread.Sleep(ProcessActivationInterval);
            }
        }

        private void DetermineIfDeletionNeeded()
        {
#if DEBUG
            SystemLogger.Singleton.LifecycleEvent("Determining if task thread '{0}' has expired.",
                _singleton == null
                    ? "Expired Task Thread"
                    : _singleton.Name);
#endif
            if (_singleton != null)
                if (_singleton._lastUsedTimeUtc < DateTime.UtcNow.Subtract(_singleton.Expiration) &&
                    _singleton.Port.ItemCount == 0 &&
                    !_singleton._isProcessingItem)
                {
                    SystemLogger.Singleton.LifecycleEvent(
                        "Task thread '{0}' has expired, please re-index Singleton to reinstantiate it.",
                        _singleton.Name);
                    _singleton.Dispose();
                    _singleton = null;
                    return;
                }
#if DEBUG
            SystemLogger.Singleton.Verbose("Task thread '{0}' has not expired.", _singleton == null ? "Expired Task Thread" : _singleton.Name);
#endif
            _singleton?._reloadTimer.Change(_singleton.Expiration, _singleton.Expiration);
        }

        #region IDisposable Members

        /// <inheritdoc/>
        public void Dispose()
        {
            _reloadTimer?.Dispose();
        }

        #endregion IDisposable Members

        #region Concurrency

        private bool _isProcessingItem;

        #endregion Concurrency

        #region Expiration Helpers

        private Timer _reloadTimer;
        private DateTime _lastUsedTimeUtc = DateTime.UtcNow;

        #endregion Expiration Helpers

        #region Other Items

        private PluginResult _lastResult;
        private int _sequenceId;
        private static TSingleton _singleton;

        #endregion Other Items
    }
}
