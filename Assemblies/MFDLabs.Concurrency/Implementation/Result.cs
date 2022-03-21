using System;
using System.Threading;

namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Represents a simple interface that will return a basic result
    /// </summary>
    /// <typeparam name="T">The typeof the result</typeparam>
    public interface IResult<T>
    {
        /// <summary>
        /// Fetch the result
        /// </summary>
        /// <returns>The <typeparamref name="T"/> that was stored in the result object.</returns>
        T GetResult();
    }

    /// <summary>
    /// Basic implementation of <see cref="IResult{T}"/>, but not inherited.
    /// </summary>
    /// <typeparam name="T">The typeof the result.</typeparam>
    public class Result<T>
    {
        private readonly Exception _exception;
        private readonly T _value;


        /// <summary>
        /// Constructs a new instance of <see cref="Result{T}"/> with <typeparamref name="T"/> as an input.
        /// </summary>
        /// <param name="value">The <typeparamref name="T"/> paremter.</param>
        public Result(T value) { _value = value; }

        /// <summary>
        /// Constructs a new instance of <see cref="Result{T}"/> with <see cref="System.Exception"/> as an input.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> paramter.</param>
        public Result(Exception exception) { _exception = exception; }

        /// <summary>
        /// Represents the <see cref="System.Exception"/> object.
        /// </summary>
        /// <returns>Returns a <see cref="System.Exception"/> object.</returns>
        public Exception Exception { get { return _exception; } }

        /// <summary>
        /// Represents the <typeparamref name="T"/> object. 
        /// If <see cref="Exception"/> is not null it will throw an exception.
        /// </summary>
        /// <returns>Returns a <typeparamref name="T"/> object.</returns>
        /// <exception cref="System.Exception">If the <see cref="Exception"/> property is present it will throw this.</exception>
        public T Value
        {
            get
            {
                if (_exception != null) throw _exception;
                return _value;
            }
        }

        /// <summary>
        /// Test the result if the <see cref="Exception"/> property is present.
        /// </summary>
        /// <param name="successHandler">The <see cref="Action{T}"/> to be invoked when the test succeeds.</param>
        /// <param name="failureHandler">The <see cref="Action{T}"/> to be invoked if the <see cref="Exception"/> property is present.</param>
        public void Test(Action<T> successHandler, Action<Exception> failureHandler)
        {
            try
            {
                if (_exception != null) throw _exception;
                successHandler?.Invoke(_value);
            }
            catch (Exception ex)
            {
                failureHandler?.Invoke(ex);
            }
        }

        /// <summary>
        /// Test the result if the <see cref="Exception"/> property is present. Will then invoke an empty delegate regardless if it fails.
        /// </summary>
        /// <param name="successHandler">The <see cref="Action{T}"/> to be invoked when the test succeeds.</param>
        /// <param name="failureHandler">The <see cref="Action{T}"/> to be invoked if the <see cref="Exception"/> property is present.</param>
        /// <param name="cleanupHandler">The <see cref="Action"/> to be invoked on try-finally block.</param>
        public void Test(Action<T> successHandler, Action<Exception> failureHandler, Action cleanupHandler)
        {
            try
            {
                if (_exception != null) throw _exception;
                successHandler?.Invoke(_value);
            }
            catch (Exception ex)
            {
                failureHandler?.Invoke(ex);
            }
            finally
            {
                cleanupHandler?.Invoke();
            }
        }
    }

    /// <summary>
    /// Simple <see cref="IAsyncResult"/> that represents Synchronous Completion.
    /// </summary>
    public class SynchronousCompletionAsyncResult : IAsyncResult
    {
        private Exception _error;
        private readonly AsyncCallback _callback;
        private bool _isCompleted = true;
        private readonly object _state;

        /// <summary>
        /// Constructs a new instance of <see cref="SynchronousCompletionAsyncResult"/> with a <see cref="AsyncCallback"/> and an <see cref="object"/>.
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> to be invoked OnEnd.</param>
        /// <param name="state">The optional state to be used.</param>
        public SynchronousCompletionAsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="SynchronousCompletionAsyncResult"/> with a <see cref="AsyncCallback"/>, an <see cref="object"/> and <see cref="bool"/> parameter to setComplete
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> to be invoked OnEnd.</param>
        /// <param name="state">The optional state to be used.</param>
        /// <param name="setComplete">A <see cref="bool"/> that determines if we should call <see cref="SetCompleted()"/></param>
        public SynchronousCompletionAsyncResult(AsyncCallback callback, object state, bool setComplete)
        {
            _callback = callback;
            _state = state;
            if (setComplete) SetCompleted();
        }

        /// <summary>
        /// Constructs a new instance of <see cref="SynchronousCompletionAsyncResult"/> with a <see cref="AsyncCallback"/>, an <see cref="object"/> and <see cref="Exception"/> parameter to setComplete with an error.
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> to be invoked OnEnd.</param>
        /// <param name="state">The optional state to be used.</param>
        /// <param name="error">A <see cref="Exception"/> that determines if we should call <see cref="SetCompleted(Exception)"/></param>
        public SynchronousCompletionAsyncResult(AsyncCallback callback, object state, Exception error)
        {
            _callback = callback;
            _state = state;
            SetCompleted(error);
        }

        /// <inheritdoc/>
        public WaitHandle AsyncWaitHandle { get { throw new NotImplementedException(); } }

        /// <inheritdoc/>
        public object AsyncState { get { return _state; } }

        /// <inheritdoc/>
        public bool CompletedSynchronously { get { return true; } }

        /// <inheritdoc/>
        public bool IsCompleted { get { return _isCompleted; } }

        /// <summary>
        /// Gets a value that represents an <see cref="Exception"/> object passed into <see cref="SetCompleted(Exception)"/>
        /// </summary>
        /// <returns>Returns an <see cref="Exception"/> object if it was set</returns>
        public Exception Error { get { return _error; } }

        /// <summary>
        /// Checks the Error property, if it's not null it will throw.
        /// </summary>
        /// <exception cref="Exception">The exception to be thrown if <see cref="Error"/> is present.</exception>
        public void CheckResult()
        {
            if (_error != null)
                throw _error;
        }

        /// <summary>
        /// Set the <see cref="IAsyncResult"/> completed and invoke the callback.
        /// </summary>
        public void SetCompleted()
        {
            _isCompleted = true;
            _callback?.Invoke(this);
        }

        /// <summary>
        /// Set the <see cref="IAsyncResult"/> completed but with an error.
        /// </summary>
        /// <param name="error">The <see cref="Exception"/> object to assign to <see cref="Error"/></param>
        public void SetCompleted(Exception error)
        {
            _error = error;
            SetCompleted();
        }
    }

    /// <summary>
    /// Implementation of <see cref="IResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The typeof the result.</typeparam>
    public class SynchronousCompletionAsyncResult<T> : SynchronousCompletionAsyncResult, IResult<T>
    {
        private readonly T _result;

        /// <summary>
        /// Constructs a new instance of <see cref="SynchronousCompletionAsyncResult{T}"/>.
        /// </summary>
        /// <param name="token">Sets the <see cref="Token"/> parameter and the value to be returned by <see cref="GetResult()"/></param>
        /// <param name="callback">The <see cref="AsyncCallback"/> to be invoked OnEnd.</param>
        /// <param name="state">The optional state to be used.</param>
        public SynchronousCompletionAsyncResult(T token, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _result = token;
            SetCompleted();
        }

        /// <summary>
        /// The <typeparamref name="T"/> result object, do not use this as it will throw.
        /// </summary>
        [Obsolete("This property can throw, which is bad design. Use GetResult() and SetCompleted() instead")]
        public T Token { get { return GetResult(); } }

        /// <inheritdoc/>
        public T GetResult()
        {
            if (Error != null) throw Error;
            return _result;
        }
    }

    /// <summary>
    /// Represents a <see cref="IAsyncResult"/> with fast operations.
    /// </summary>
    public class FastAsyncResult : IAsyncResult, IDisposable
    {
        private Exception _error;
        private readonly AsyncCallback _callback;
        private bool _isCompleted = true;
        private readonly object _state;
        private ManualResetEvent _waitHandle;

        /// <summary>
        /// Constructs a new <see cref="FastAsyncResult"/>.
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> to be invoked.</param>
        /// <param name="state">The optional state.</param>
        public FastAsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
        }

        /// <inheritdoc/>
        public WaitHandle AsyncWaitHandle { get { return CreateWaitHandle(); } }

        /// <inheritdoc/>
        public object AsyncState { get { return _state; } }

        /// <inheritdoc/>
        public bool CompletedSynchronously { get { return false; } }

        /// <inheritdoc/>
        public bool IsCompleted { get { return _isCompleted; } }

        /// <summary>
        /// Gets a value that represents an <see cref="Exception"/> object passed into <see cref="SetCompleted(Exception)"/>
        /// </summary>
        /// <returns>Returns an <see cref="Exception"/> object if it was set</returns>
        public Exception Error { get { return _error; } }

        private WaitHandle CreateWaitHandle()
        {
            if (_waitHandle != null) return _waitHandle;

            var resetEvt = new ManualResetEvent(false);
            if (Interlocked.CompareExchange(ref _waitHandle, resetEvt, null) != null) resetEvt.Close();
            if (_isCompleted) _waitHandle.Set();
            return _waitHandle;
        }

        /// <inheritdoc/>
        public void Dispose() { GC.SuppressFinalize(this); _waitHandle?.Close(); }

        /// <summary>
        /// Set the <see cref="IAsyncResult"/> completed and invoke the callback.
        /// </summary>
        public void SetCompleted()
        {
            _isCompleted = true;
            Thread.MemoryBarrier();
            _waitHandle?.Set();
            _callback?.Invoke(this);
        }

        /// <summary>
        /// Set the <see cref="IAsyncResult"/> completed but with an error.
        /// </summary>
        /// <param name="error">The <see cref="Exception"/> object to assign to <see cref="Error"/></param>
        public void SetCompleted(Exception error)
        {
            _error = error;
            SetCompleted();
        }

        /// <summary>
        /// Sets the <see cref="Error"/> property
        /// </summary>
        /// <param name="error">The <see cref="Exception"/> object to assign to <see cref="Error"/></param>
        public void SetFailed(Exception error) { _error = error; }
    }

    /// <summary>
    /// Implementation of <see cref="IResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The typeof the result.</typeparam>
    public class FastAsyncResult<T> : FastAsyncResult, IResult<T>
    {
        private T _result;

        /// <summary>
        /// Constructs a new <see cref="FastAsyncResult{T}"/> object.
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> to be invoked.</param>
        /// <param name="state">The optional state.</param>
        public FastAsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        /// <summary>
        /// The <typeparamref name="T"/> result object, do not use this as it will throw.
        /// </summary>
        [Obsolete("This property can throw, which is bad design. Use GetResult() and SetCompleted() instead")]
        public T Token { get { return GetResult(); } set { _result = value; } }

        /// <inheritdoc/>
        public T GetResult()
        {
            if (Error != null) throw Error;
            return _result;
        }

        /// <summary>
        /// Sets the <see cref="IAsyncResult"/> completed with a <typeparamref name="T"/> result.
        /// </summary>
        /// <param name="result">The <typeparamref name="T"/> result to set.</param>
        public void SetCompleted(T result)
        {
            _result = result;
            SetCompleted();
        }
    }
}
