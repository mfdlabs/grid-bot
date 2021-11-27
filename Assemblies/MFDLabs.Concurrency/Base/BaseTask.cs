using System;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using Microsoft.Ccr.Core;

namespace MFDLabs.Concurrency.Base
{
    /// <summary>
    /// A <see cref="BasePlugin{TSingleton, TItem}"/> for receivers
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    /// <typeparam name="TItem">The typeof the <typeparamref name="TItem"/> to use</typeparam>
    public abstract class BaseTask<TSingleton, TItem> : BasePlugin<TSingleton, TItem>
        where TSingleton : BaseTask<TSingleton, TItem>, new()
        where TItem : class
    {
        #region Members

        /// <summary>
        /// The name of this <see cref="BaseTask{TSingleton, TItem}"/> and the <see cref="TaskThreadMonitor"/> when monitoring performance.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The <see cref="ICounterRegistry"/> to be used by the task with the <see cref="TaskThreadMonitor"/>.
        /// </summary>
        public abstract ICounterRegistry CounterRegistry { get; }

        /// <summary>
        /// The <see cref="IPacket.ID"/> to be set when creating new <see cref="Packet{TItem}"/>s
        /// </summary>
        public abstract int PacketID { get; }

        /// <summary>
        /// The <see cref="Port{T}"/> to be used when receiving via <see cref="Arbiter.Receive{T}(bool, Port{T}, Handler{T})"/>.
        /// </summary>

        public readonly Port<TItem> Port = new Port<TItem>();

        /// <summary>
        /// A boolean that determines if the current task is allowed to receive new members.
        /// Set to false when the last <see cref="PluginResult"/> is <see cref="PluginResult.StopProcessingAndDeallocate"/>.
        /// </summary>
        public bool CanReceive { get; protected set; } = true;

        #endregion Members

        /// <inheritdoc/>
        public BaseTask()
        {
            _monitor = new TaskThreadMonitor(CounterRegistry, Name);
        }

        /// <summary>
        /// Actives an item inside the <see cref="Port{T}"/>, it doesn't care about if there are items inside of it.
        /// </summary>
        /// <returns>Returns the last <see cref="PluginResult"/>.</returns>
        public PluginResult Activate()
        {
            if (CanReceive)
            {
                lock (_lock)
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
                            }
                        )
                    );
                }
                lock (_resultLock)
                    if (_lastResult == PluginResult.StopProcessingAndDeallocate)
                    {
                        Deallocate();
                    }
            }
            return _lastResult;
        }

        private void Deallocate()
        {
            CanReceive = false;
        }

        #region Concurrency

        private readonly object _resultLock = new object();
        private readonly object _lock = new object();

        #endregion Concurrency

        #region Other Items

        private int _sequenceID = 0;
        private PluginResult _lastResult;
        /// <inheritdoc/>
        protected readonly TaskThreadMonitor _monitor;

        #endregion
    }
}
