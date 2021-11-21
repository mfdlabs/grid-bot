using MFDLabs.Abstractions;

namespace MFDLabs.Concurrency.Base.Unsafe
{
    /// <summary>
    /// An <see cref="UnsafeBasePlugin{TSingleton}"/> for unsafe style receivers with no item.
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    public abstract class UnsafeBasePlugin<TSingleton> : SingletonBase<TSingleton>
        where TSingleton : UnsafeBasePlugin<TSingleton>, new()
    {
        /// <summary>
        /// The method to be invoked when the <see cref="Microsoft.Ccr.Core.Arbiter"/> calls <see cref="Microsoft.Ccr.Core.Arbiter.Activate(Microsoft.Ccr.Core.DispatcherQueue, Microsoft.Ccr.Core.ITask[])"/> with a new <see cref="Packet"/>.
        /// </summary>
        /// <param name="packet">The <see cref="IPacket"/> to be used when executing the task.</param>
        /// <returns>Returns a <see cref="PluginResult"/></returns>
        public unsafe abstract PluginResult OnReceive(global::MFDLabs.Concurrency.Unsafe.Packet* packet);
    }
}
