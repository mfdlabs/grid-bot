using System;
using System.Diagnostics.CodeAnalysis;
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
        protected abstract string Name { get; }

        /// <summary>
        /// The <see cref="ICounterRegistry"/> to be used by the task with the <see cref="TaskThreadMonitor"/>.
        /// </summary>
        protected abstract ICounterRegistry CounterRegistry { get; }

        /// <summary>
        /// The <see cref="IPacket.Id"/> to be set when creating new <see cref="Packet{TItem}"/>s
        /// </summary>
        protected abstract int PacketId { get; }

        /// <summary>
        /// The <see cref="Port{T}"/> to be used when receiving via <see cref="Arbiter.Receive{T}(bool, Port{T}, Handler{T})"/>.
        /// </summary>

        public readonly Port<TItem> Port = new Port<TItem>();

        /// <summary>
        /// A boolean that determines if the current task is allowed to receive new members.
        /// Set to false when the last <see cref="PluginResult"/> is <see cref="PluginResult.StopProcessingAndDeallocate"/>.
        /// </summary>
        protected bool CanReceive { get; set; } = true;

        #endregion Members

        /// <inheritdoc/>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected BaseTask()
        {
            Monitor = new TaskThreadMonitor(CounterRegistry, Name);
        }

        /// <summary>
        /// Actives an item inside the <see cref="Port{T}"/>, it doesn't care about if there are items inside of it.
        /// </summary>
        /// <returns>Returns the last <see cref="PluginResult"/>.</returns>
        public PluginResult Activate()
        {
            if (!CanReceive) return _lastResult;
            
            ConcurrencyService.Singleton.Activate(
                Arbiter.Receive(
                    false,
                    Port,
                    (item) =>
                    {
                        _sequenceId++;
                        Monitor.CountOfItemsProcessed.Increment();
                        Monitor.RateOfItemsPerSecondProcessed.Increment();
                        Monitor.AverageRateOfItems.Sample(1.0 / _sequenceId);
                        try
                        {
                            var packet = new Packet<TItem>(item, PacketId, _sequenceId, Monitor);
                            _lastResult = OnReceive(ref packet);
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
                            packet.Dispose();
                            
                            if (_lastResult == PluginResult.StopProcessingAndDeallocate)
                            {
                                Deallocate();
                            }
                        }
                        catch (Exception ex)
                        {
                            Monitor.CountOfItemsProcessedThatFail.Increment();
                            Monitor.RateOfItemsPerSecondProcessedThatFail.Increment();
                            Monitor.AverageRateOfItemsThatFail.Sample(1.0 / _sequenceId);

#if DEBUG || DEBUG_LOGGING_IN_PROD
                            SystemLogger.Singleton.Error(ex);
#else
                            SystemLogger.Singleton.Warning("An error occurred when trying to process a received task item: {0}", ex.Message);
#endif
                        }
                    }
                )
            );
            
            return _lastResult;
        }

        private void Deallocate()
        {
            CanReceive = false;
        }

        #region Other Items

        private int _sequenceId;
        private PluginResult _lastResult;
        /// <summary>
        /// Monitor
        /// </summary>
        protected readonly TaskThreadMonitor Monitor;

        #endregion
    }
}
