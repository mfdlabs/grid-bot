using System;
using System.Diagnostics.CodeAnalysis;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using Microsoft.Ccr.Core;

namespace MFDLabs.Concurrency.Base.Unsafe
{
    /// <summary>
    /// A <see cref="UnsafeBasePlugin{TSingleton}"/> for async style receivers
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    /// <typeparam name="TItem">The typeof the <typeparamref name="TItem"/></typeparam>
    public abstract class UnsafeBaseTask<TSingleton, TItem> : UnsafeBasePlugin<TSingleton>
        where TSingleton : UnsafeBaseTask<TSingleton, TItem>, new()
        where TItem : class
    {
        #region Members

        /// <summary>
        /// The name of this <see cref="UnsafeBasePlugin{TSingleton}"/> and the <see cref="TaskThreadMonitor"/> when recording performance.
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

        /// <summary>
        /// </summary>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected UnsafeBaseTask()
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
                    }
                )
            );
            if (_lastResult == PluginResult.StopProcessingAndDeallocate)
            {
                Deallocate();
            }

            return _lastResult;
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected unsafe byte* GetRawDataBuffer(TItem item)
        {
            byte[] data;

            using (var stream = new System.IO.MemoryStream())
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(stream, item);
                data = stream.ToArray();
            }

            fixed (byte* d = data) return d;
        }

        private void Deallocate()
        {
            CanReceive = false;
        }

        #region Other Items

        private int _sequenceId;
        private PluginResult _lastResult;

        /// <summary>
        /// </summary>
        protected readonly TaskThreadMonitor Monitor;

        #endregion Other Items
    }
}