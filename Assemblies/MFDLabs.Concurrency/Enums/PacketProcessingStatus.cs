namespace MFDLabs.Concurrency
{
    /// <summary>
    /// The status for a <see cref="Packet"/> when recording metrics via <see cref="TaskThreadMonitor"/>
    /// </summary>
    public enum PacketProcessingStatus
    {
        /// <summary>
        /// Does nothing.
        /// </summary>
        Success,

        /// <summary>
        /// If the <see cref="Packet.Status"/> is <see cref="Failure"/> then the <see cref="TaskThreadMonitor.AverageRateOfItemsThatFail"/> etc will increment and be sampled.
        /// </summary>
        Failure
    }
}
