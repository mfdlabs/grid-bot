using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Concurrency.Base.Async
{
    /// <summary>
    /// The base task thread but async!! :)
    /// </summary>
    /// <typeparam name="TSingleton">the implementated class, must be base task thread or of base task thread.</typeparam>
    /// <typeparam name="TItem">the item in the port</typeparam>
    public abstract class AsyncBaseTaskThread<TSingleton, TItem> : AsyncBaseTask<TSingleton, TItem>
        where TSingleton : AsyncBaseTaskThread<TSingleton, TItem>, new()
        where TItem : class
    {
        #region Members

        /// <summary>
        /// A timeout for each activation of the <see cref="AsyncBaseTask{TSingleton, TItem}.Port"/>.
        /// </summary>
        protected virtual TimeSpan ProcessActivationInterval { get; } = TimeSpan.FromSeconds(1.5);

        #endregion Members

        /// <summary>
        /// </summary>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected AsyncBaseTaskThread() => Initialize();

        /// <summary>
        /// Initialize the task thread.
        /// </summary>
        protected virtual void Initialize()
        {
            if (Name.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Error("There was a null Task Thread name, please review it to make it not null.");
                return;
            }

            SystemLogger.Singleton.LifecycleEvent("Initializing Task Thread '{0}'.", Name);

            if (!_initialized)
            {
                lock (_initLock)
                {
                    new Thread(ThreadWorker)
                    {
                        IsBackground = true,
                        Name = Name
                    }.Start();
                }

                _initialized = true;
            }

            SystemLogger.Singleton.Verbose("Initialized Task Thread '{0}'.", Name);
        }

        /// <summary>
        /// The thread worker callback.
        /// </summary>
        protected virtual void ThreadWorker()
        {
            SystemLogger.Singleton.Debug("Starting '{0}' with the delay of '{1}'", Name, ProcessActivationInterval);

            while (true)
            {
                if (Port.ItemCount > 0)
                {
                    var result = Activate();
                    if (result == PluginResult.StopProcessingAndDeallocate) break;
                }

                Thread.Sleep(ProcessActivationInterval);
            }
        }

        #region Concurrency

        private readonly object _initLock = new object();
        private bool _initialized;

        #endregion
    }
}
