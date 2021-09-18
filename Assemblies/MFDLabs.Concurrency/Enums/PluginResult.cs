namespace MFDLabs.Concurrency
{
    /// <summary>
    /// The result of the <see cref="Base.BasePlugin{TSingleton}.OnReceive(ref Packet)"/> and <see cref="Base.Async.AsyncBasePlugin{TSingleton}.OnReceive(Packet)"/>
    /// </summary>
    public enum PluginResult
    {
        /// <summary>
        /// The <see cref="Base.BaseTask{TSingleton, TItem}"/> or <see cref="Base.Async.AsyncBaseTask{TSingleton, TItem}"/> should continue processing.
        /// </summary>
        ContinueProcessing,

        /// <summary>
        /// The <see cref="Base.BaseTask{TSingleton, TItem}"/> or <see cref="Base.Async.AsyncBaseTask{TSingleton, TItem}"/> should stop processing at deallocate itself.
        /// </summary>
        StopProcessingAndDeallocate
    }
}
