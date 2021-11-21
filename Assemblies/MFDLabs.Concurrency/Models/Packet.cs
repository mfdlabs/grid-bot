using System;
using System.Diagnostics;

namespace MFDLabs.Concurrency
{
    namespace Unsafe
    {
        /// <inheritdoc />
        public unsafe struct Packet
        {
            /// <inheritdoc />
            public int id { get; set; }

            /// <inheritdoc />
            public int sequence_id { get; set; }

            /// <inheritdoc />
            public byte* data { get; set; }

            /// <inheritdoc />
            public DateTime created { get; set; }

            /// <inheritdoc />
            public PacketProcessingStatus status { get; set; }
        }
    }

    /// <summary>
    /// The <see cref="IPacket"/> to be used with factories.
    /// </summary>
    public interface IPacket
    {
        /// <summary>
        /// The <see cref="ID"/> of the <see cref="IPacket"/> when receiving.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// The <see cref="SequenceID"/> of the <see cref="IPacket"/> when creating.
        /// </summary>
        int SequenceID { get; }

        /// <summary>
        /// The <see cref="DateTime"/> for when the <see cref="IPacket"/> was created.
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// A <see cref="TaskThreadMonitor"/> to be used to record performance for the current <see cref="IPacket"/>
        /// </summary>
        TaskThreadMonitor PerformanceMonitor { get; }

        /// <summary>
        /// The <see cref="PacketProcessingStatus"/> of the <see cref="IPacket"/> when recording metrics via <see cref="PerformanceMonitor"/>
        /// </summary>
        PacketProcessingStatus Status { get; set; }
    }

    /// <summary>
    /// The <see cref="IPacket{TItem}"/> to be used with factories with an item.
    /// </summary>
    public interface IPacket<TItem> : IPacket, IDisposable
        where TItem : class
    {
        /// <summary>
        /// The <see cref="Item"/> of the <see cref="IPacket{TItem}"/>
        /// </summary>
        TItem Item { get; }
    }

    /// <summary>
    /// The <see cref="IPacket"/> implementation.
    /// </summary>
    [DebuggerDisplay("ID = {ID}, SequenceID = {SequenceID}, Created = {Created}, Status = {Status}")]
    public class Packet : IPacket, IDisposable
    {
        /// <inheritdoc/>
        public int ID
        {
            get => _id;
        }

        /// <inheritdoc/>
        public int SequenceID
        {
            get => _sequenceid;
        }

        /// <inheritdoc/>
        public DateTime Created
        {
            get => _createdUtc;
        }

        /// <inheritdoc/>
        public TaskThreadMonitor PerformanceMonitor
        {
            get => _perfmon?.Value;
        }

        /// <inheritdoc/>
        public PacketProcessingStatus Status { get; set; } = PacketProcessingStatus.Success;

        internal Packet(int id, int sequenceID, TaskThreadMonitor perfmon)
        {
            _perfmon = new Lazy<TaskThreadMonitor>(() => perfmon, true);
            _sequenceid = sequenceID;
            _createdUtc = DateTime.UtcNow;
            _id = id;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        #region Private Members

        private readonly Lazy<TaskThreadMonitor> _perfmon;
        private readonly int _sequenceid;
        private readonly DateTime _createdUtc;
        private readonly int _id;

        #endregion Private Members
    }

    /// <summary>
    /// The <see cref="IPacket{TItem}"/> implementation.
    /// </summary>
    /// <typeparam name="TItem">The item of the packet.</typeparam>
    public sealed class Packet<TItem> : Packet, IPacket<TItem>, IPacket, IDisposable
        where TItem : class
    {
        /// <inheritdoc/>
        public TItem Item
        {
            get => _item?.Value;
        }

        internal Packet(TItem item, int id, int sequenceID, TaskThreadMonitor perfmon)
            : base(id, sequenceID, perfmon)
        {
            _item = new Lazy<TItem>(() => item, true);
        }

        #region Private Members

        private readonly Lazy<TItem> _item;

        #endregion Private Members
    }
}
