using System.Threading.Tasks;
using MFDLabs.Abstractions;

namespace MFDLabs.Concurrency.Base.Async
{
    /// <summary>
    /// An <see cref="AsyncBasePlugin{TSingleton}"/> for async style receivers with no item.
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    public abstract class AsyncBasePlugin<TSingleton> : SingletonBase<TSingleton>
        where TSingleton : AsyncBasePlugin<TSingleton>, new()
    {
        /// <summary>
        /// The method to be invoked when the <see cref="Microsoft.Ccr.Core.Arbiter"/> calls <see cref="Microsoft.Ccr.Core.Arbiter.Activate(Microsoft.Ccr.Core.DispatcherQueue, Microsoft.Ccr.Core.ITask[])"/> with a new <see cref="Packet"/>.
        /// </summary>
        /// <param name="packet">The <see cref="IPacket"/> to be used when executing the task.</param>
        /// <returns>Returns a <see cref="Task{PluginResult}"/> to be awaited by tasks and task threads.</returns>
        public abstract Task<PluginResult> OnReceive(Packet packet);
    }

    /// <summary>
    /// An <see cref="AsyncBasePlugin{TSingleton, TItem}"/> for async style receivers with an item.
    /// </summary>
    /// <typeparam name="TSingleton">The typeof the <typeparamref name="TSingleton"/></typeparam>
    /// <typeparam name="TItem">The typeof the <typeparamref name="TItem"/> to use</typeparam>
    public abstract class AsyncBasePlugin<TSingleton, TItem> : SingletonBase<TSingleton>
        where TSingleton : AsyncBasePlugin<TSingleton, TItem>, new()
        where TItem : class
    {
        /// <summary>
        /// The method to be invoked when the <see cref="Microsoft.Ccr.Core.Arbiter"/> calls <see cref="Microsoft.Ccr.Core.Arbiter.Receive{T}(bool, Microsoft.Ccr.Core.Port{T}, Microsoft.Ccr.Core.Handler{T})"/> with a new <see cref="Packet{TItem}"/>.
        /// </summary>
        /// <param name="packet">The <see cref="IPacket{TItem}"/> to be used when executing the task.</param>
        /// <returns>Returns a <see cref="Task{PluginResult}"/> to be awaited by tasks and task threads.</returns>
        public abstract Task<PluginResult> OnReceive(Packet<TItem> packet);
    }
}
