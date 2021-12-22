using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Simple fast async result.
    /// </summary>
    public class FastAsyncResult : IAsyncResult, IDisposable
    {
        // Fields
        private readonly object _state;
        private ManualResetEvent _waitHandle;
        private bool _isCompleted;
        private readonly AsyncCallback _callback;

        // Constructors
        /// <summary>
        /// Construct a new FastAsyncResult
        /// </summary>
        /// <param name="callback">On AsyncCallbackInvoked</param>
        /// <param name="state">State</param>
        public FastAsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
        }

        // Properties

        /// <inheritdoc/>
        public object AsyncState => _state;

        /// <summary>
        /// wait handle
        /// </summary>
        public WaitHandle AsyncWaitHandle => LazyCreateWaitHandle();

        /// <inheritdoc/>
        public bool CompletedSynchronously => false;

        /// <inheritdoc/>
        public bool IsCompleted => _isCompleted;

        // Methods

        /// <inheritdoc/>
        public void Dispose() => _waitHandle?.Close();

        /// <summary>
        /// Set complete
        /// </summary>
        public void SetComplete()
        {
            // We set the boolean first.
            _isCompleted = true;

            // And then, if the wait handle was created, we need to signal it.  Note the
            // use of a memory barrier. This is required to ensure the read of m_waitHandle
            // never moves before the store of m_isCompleted; otherwise we might encounter a
            // race that leads us to not signal the handle, leading to a deadlock.  We can't
            // just do a volatile read of m_waitHandle, because it is possible for an acquire
            // load to move before a store release.

            Thread.MemoryBarrier();

            _waitHandle?.Set();

            // If the callback is non-null, we invoke it.
            _callback?.Invoke(this);
        }

        private WaitHandle LazyCreateWaitHandle()
        {
            if (_waitHandle != null)
            {
                return _waitHandle;
            }

            var newHandle = new ManualResetEvent(false);
            if (Interlocked.CompareExchange(
                    ref _waitHandle, newHandle, null) != null)
            {
                // We lost the race. Release the handle we created, it's garbage.
                newHandle.Close();
            }

            if (_isCompleted)
            {
                // If the result has already completed, we must ensure we return the
                // handle in a signaled state. The read of m_isCompleted must never move
                // before the read of m_waitHandle earlier; the use of an interlocked
                // compare-exchange just above ensures that. And there's a race that could
                // lead to multiple threads setting the event; that's no problem.
                _waitHandle.Set();
            }

            return _waitHandle;
        }
    }
}
