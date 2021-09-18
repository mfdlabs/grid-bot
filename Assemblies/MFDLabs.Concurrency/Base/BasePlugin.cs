using MFDLabs.Abstractions;

namespace MFDLabs.Concurrency.Base
{
    /// <summary>
    /// An <see cref="BasePlugin{TSingleton}"/> for receivers with no item.
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    public abstract class BasePlugin<TSingleton> : SingletonBase<TSingleton>
        where TSingleton : BasePlugin<TSingleton>, new()
    {
        /// <summary>
        /// The method to be invoked when the <see cref="Microsoft.Ccr.Core.Arbiter"/> calls <see cref="Microsoft.Ccr.Core.Arbiter.Activate(Microsoft.Ccr.Core.DispatcherQueue, Microsoft.Ccr.Core.ITask[])"/> with a new reference <see cref="Packet"/>.
        /// </summary>
        /// <param name="packet">The reference <see cref="IPacket"/> to be used when executing the task.</param>
        /// <returns>Returns a <see cref="PluginResult"/> that determines if the task should continue processing, or should stop processing.</returns>
        public abstract PluginResult OnReceive(ref Packet packet);
    }

    /// <summary>
    /// An <see cref="BasePlugin{TSingleton, TItem}"/> for receivers with an item.
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    /// <typeparam name="TItem">The typeof the <typeparamref name="TItem"/> to use</typeparam>
    public abstract class BasePlugin<TSingleton, TItem> : SingletonBase<TSingleton>
        where TSingleton : BasePlugin<TSingleton, TItem>, new()
        where TItem : class
    {
        /// <summary>
        /// The method to be invoked when the <see cref="Microsoft.Ccr.Core.Arbiter"/> calls <see cref="Microsoft.Ccr.Core.Arbiter.Activate(Microsoft.Ccr.Core.DispatcherQueue, Microsoft.Ccr.Core.ITask[])"/> with a new reference <see cref="Packet{TItem}"/>.
        /// </summary>
        /// <param name="packet">The reference <see cref="IPacket{TItem}"/> to be used when executing the task.</param>
        /// <returns>Returns a <see cref="PluginResult"/> that determines if the task should continue processing, or should stop processing.</returns>
        public abstract PluginResult OnReceive(ref Packet<TItem> packet);
    }
}
