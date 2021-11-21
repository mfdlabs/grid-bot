using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
        public abstract string Name { get; }

        /// <summary>
        /// The <see cref="ICounterRegistry"/> to be used by the task with the <see cref="TaskThreadMonitor"/>.
        /// </summary>
        public virtual ICounterRegistry CounterRegistry { get; } = StaticCounterRegistry.Instance;

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

        /// <summary>
        /// </summary>
        public UnsafeBaseTask()
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
                            try
                            {
                                unsafe
                                {
                                    var packet = new MFDLabs.Concurrency.Unsafe.Packet
                                    {
                                        id = PacketID,
                                        sequence_id = _sequenceID,
                                        data = GetRawDataBuffer(item),
                                        created = DateTime.Now,
                                        status = PacketProcessingStatus.Success
                                    };

                                    var pkt = packet.ToPtr();

                                    _lastResult = OnReceive(pkt);
                                    if (packet.status == PacketProcessingStatus.Failure)
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
                                }
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
                    )
                );
                if (_lastResult == PluginResult.StopProcessingAndDeallocate)
                {
                    Deallocate();
                }
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

        private int _sequenceID = 0;
        private PluginResult _lastResult;

        /// <summary>
        /// </summary>
        protected readonly TaskThreadMonitor _monitor;

        #endregion Other Items
    }
}