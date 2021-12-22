using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    namespace Unsafe
    {
        /// <summary>
        /// Unsafe packet
        /// </summary>
        public unsafe struct Packet
        {
            /// <summary>
            /// Packet ID
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Packet Sequence ID
            /// </summary>
            public int SequenceId { get; set; }

            /// <summary>
            /// Raw packet Data
            /// </summary>
            public byte* Data { get; set; }

            /// <summary>
            /// When created
            /// </summary>
            public DateTime Created { get; set; }

            /// <summary>
            /// Execution status
            /// </summary>
            public PacketProcessingStatus Status { get; set; }
        }
    }

    /// <summary>
    /// The <see cref="IPacket"/> to be used with factories.
    /// </summary>
    public interface IPacket
    {
        /// <summary>
        /// The <see cref="Id"/> of the <see cref="IPacket"/> when receiving.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The <see cref="SequenceId"/> of the <see cref="IPacket"/> when creating.
        /// </summary>
        int SequenceId { get; }

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
    public interface IPacket<out TItem> : IPacket, IDisposable
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
    [DebuggerDisplay("ID = {Id}, SequenceID = {SequenceId}, Created = {Created}, Status = {Status}")]
    public class Packet : IPacket, IDisposable
    {
        /// <inheritdoc/>
        public int Id => _id;

        /// <inheritdoc/>
        public int SequenceId => _sequenceid;

        /// <inheritdoc/>
        public DateTime Created => _createdUtc;

        /// <inheritdoc/>
        public TaskThreadMonitor PerformanceMonitor => _perfmon?.Value;

        /// <inheritdoc/>
        public PacketProcessingStatus Status { get; set; } = PacketProcessingStatus.Success;

        internal Packet(int id, int sequenceId, TaskThreadMonitor perfmon)
        {
            _perfmon = new Lazy<TaskThreadMonitor>(() => perfmon, true);
            _sequenceid = sequenceId;
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
    public sealed class Packet<TItem> : Packet, IPacket<TItem>
        where TItem : class
    {
        /// <inheritdoc/>
        public TItem Item => _item?.Value;

        internal Packet(TItem item, int id, int sequenceId, TaskThreadMonitor perfmon)
            : base(id, sequenceId, perfmon)
        {
            _item = new Lazy<TItem>(() => item, true);
        }

        #region Private Members

        private readonly Lazy<TItem> _item;

        #endregion Private Members
    }
}
