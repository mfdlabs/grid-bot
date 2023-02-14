/*

    File name: GridServerInstance.cs
    Written By: @networking-owk
    Description: Represents the base class for a grid server instance to be consumed by the arbiter.

    Copyright MFDLABS 2001-2022. All rights reserved.

*/

/*

Notes from feature/grid-server-recovery-pt2:

- Cases where auto-recovery can occur:
    - EndpointNotFoundException
    - CommunicationException but not for the following:
        - The fault returned was invalid.
    - TimeoutException
    - A potential deadlock was detected.
    - FaultException in the following cases:
        - Cannot batch job while another job is running.
        - Batch job timed out.

*/

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#define DEBUG_LOGGING_IN_PROD

namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ServiceModel.Channels;

using Logging;
using ComputeCloud;
using Instrumentation;

/// <summary>
/// Base class for a grid server instance to be consumed by the arbiter.
/// </summary>
[DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
public class GridServerInstance : ComputeCloudServiceSoapClient, IDisposable, IGridServerInstance
{
    #region |Instrumentation|

    /// <summary>
    /// Performance monitor for grid server instances.
    /// </summary>
    protected class GridServerInstancePerformanceMonitor
    {
        private const string Category = "MFDLabs.Grid.Arbiter.Instance";

        /// <summary>
        /// Total SOAP invocations.
        /// </summary>
        public IRawValueCounter TotalInvocations { get; }

        /// <summary>
        /// Total SOAP invocations that succeed.
        /// </summary>
        public IRawValueCounter TotalInvocationsThatSucceeded { get; }

        /// <summary>
        /// Total SOAP invocations that fail.
        /// </summary>
        public IRawValueCounter TotalInvocationsThatFailed { get; }

        /// <summary>
        /// Construct a new instance of <see cref="GridServerInstancePerformanceMonitor"/>
        /// </summary>
        /// <param name="counterRegistry">The <see cref="ICounterRegistry"/> for instrumentation.</param>
        /// <param name="instance">The <see cref="GridServerInstance"/> for specific instance boil-down.</param>
        /// <exception cref="ArgumentNullException">
        /// - <paramref name="counterRegistry"/> cannot be null.
        /// - <paramref name="instance"/> cannot be null.
        /// </exception>
        public GridServerInstancePerformanceMonitor(ICounterRegistry counterRegistry, GridServerInstance instance)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            TotalInvocations = counterRegistry.GetRawValueCounter(Category, "TotalInvocations", instance.Name);
            TotalInvocationsThatSucceeded = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatSucceeded", instance.Name);
            TotalInvocationsThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalInvocationsThatFailed", instance.Name);
        }
    }

    #endregion |Instrumentation|

    #region |Private Members|

    /// <summary>
    /// The owner of this grid server instance.
    /// </summary>
    protected readonly IGridServerArbiter GridServerArbiter;

    /// <summary>
    /// The deployer of this grid server instance's process.
    /// </summary>
    protected readonly IGridServerDeployer GridServerDeployer;

    /// <summary>
    /// The logger for this grid server instance.
    /// </summary>
    protected readonly ILogger Logger;

    private readonly string _name;
    private readonly bool _isPoolable;
    private readonly bool _isPersistent;
    private readonly int _maxAttemptsToCallSoap;
    private TaskCompletionSource<bool> _availableWaitHandle;

    /// <summary>
    /// Performance monitor for inherited classes.
    /// </summary>
    protected readonly GridServerInstancePerformanceMonitor PerformanceMonitor;

    #region |Thread-Safe Members|

    private readonly object _availableLock = new();
    private readonly object _openLock = new();

    #endregion |Thread-Safe Members|

    #region |Mutable|

    private volatile bool _isAvailable;
    private volatile bool _isDisposed;
    private volatile IGridServerProcess _gridServerProcess; /* Applied either from constructor or in TryOpen() */

    #endregion |Mutable|

    #endregion |Private Members|

    #region |Informative Members|

    /// <inheritdoc cref="IGridServerInstance.Name"/>
    public string Name => _name;

    /// <inheritdoc cref="IGridServerInstance.IsAvailable"/>
    public bool IsAvailable
    {
        get
        {
            lock (_availableLock)
                return _isAvailable && !_isDisposed;
        }
    }

    /// <inheritdoc cref="IGridServerInstance.IsOpened"/>
    public bool IsOpened => _gridServerProcess != null && _gridServerProcess.IsOpen;

    /// <inheritdoc cref="IGridServerInstance.Persistent"/>
    public bool Persistent => _isPersistent;

    /// <inheritdoc cref="IGridServerInstance.IsPoolable"/>
    public bool IsPoolable => _isPoolable;

    /// <inheritdoc cref="IGridServerInstance.IsDisposed"/>
    public bool IsDisposed => _isDisposed;

    /// <inheritdoc cref="IGridServerInstance.Process"/>
    public IGridServerProcess Process => _gridServerProcess;

    #endregion |Informative Members|

    #region |Contructors|

    /// <summary>
    /// Construct a new instance of <see cref="GridServerInstance"/>.
    /// </summary>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/> to use for instrumentation.</param>
    /// <param name="logger">The <see cref="ILogger"/> for debug logging.</param>
    /// <param name="gridServerArbiter">The <see cref="IGridServerArbiter"/> that created this instance.</param>
    /// <param name="gridServerDeployer">The <see cref="IGridServerDeployer"/> for managing the <see cref="IGridServerProcess"/>.</param>
    /// <param name="httpBinding">The <see cref="Binding"/> given to <see cref="IGridServerArbiter"/> to interact with WCF.</param>
    /// <param name="remoteAddress">The remote address of the instance.</param>
    /// <param name="name">The name of the instance.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum attempts this instance can initiate a failing SOAP call before it throws a <see cref="TimeoutException"/>.</param>
    /// <param name="isPersistent">Is this instance persistent across runs?</param>
    /// <param name="isPoolable">Is this instance in the arbiter pool?</param>
    /// <param name="startNow">Start the <see cref="IGridServerProcess"/> now? Opens it in a new thread.</param>
    /// <param name="gridServerProcess">The optional <see cref="IGridServerProcess"/> to assign if there is an existing one.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="counterRegistry"/> cannot be null.
    /// - <paramref name="gridServerArbiter"/> cannot be null.
    /// - <paramref name="gridServerDeployer"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> cannot be null or white-space.</exception>
    internal GridServerInstance(
        ICounterRegistry counterRegistry,
        ILogger logger,
        IGridServerArbiter gridServerArbiter,
        IGridServerDeployer gridServerDeployer,
        Binding httpBinding,
        EndpointAddress remoteAddress,
        string name,
        int maxAttemptsToCallSoap = 5,
        bool isPersistent = false,
        bool isPoolable = true,
        bool startNow = true,
        IGridServerProcess gridServerProcess = null // Optional for Arbiter.Discover()
    )
        : base(httpBinding, remoteAddress)
    {
        if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"{nameof(name)} cannot be null or white-space.", nameof(name));

        GridServerArbiter = gridServerArbiter ?? throw new ArgumentNullException(nameof(gridServerArbiter));
        GridServerDeployer = gridServerDeployer ?? throw new ArgumentNullException(nameof(gridServerDeployer));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _name = name;
        _isPoolable = isPoolable;
        _isPersistent = isPersistent;
        _maxAttemptsToCallSoap = maxAttemptsToCallSoap;

        PerformanceMonitor = new GridServerInstancePerformanceMonitor(counterRegistry, this);

        _gridServerProcess = gridServerProcess;

        _availableWaitHandle = new TaskCompletionSource<bool>();
        _availableWaitHandle.TrySetResult(true);

        if (startNow && !(gridServerProcess != null && gridServerProcess.IsOpen))
            Task.Run(TryStart);
    }

    #endregion |Contructors|

    #region |LifeCycle Managment Helpers|

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (IsDisposed) return;

        GC.SuppressFinalize(this);
        Logger.LifecycleEvent("Closing instance '{0}'...", _name);

        if (_gridServerProcess != null)
            GridServerDeployer.KillProcess(_gridServerProcess, out _); // We do not care about the result or exception.

        GridServerArbiter.RemoveInstance(this);

        _availableWaitHandle.TrySetResult(true);

        _isDisposed = true;
    }

    /// <inheritdoc cref="IGridServerInstance.TryStart"/>
    public virtual bool TryStart()
    {
        lock (_openLock)
        {
            if (IsOpened) return true;

            if (!GridServerDeployer.CreateProcess(Endpoint.Address.Uri.Port, out var gridServerProcess, out var exception))
            {
                if (exception != null)
                    Logger.Error("Failed to create process for {0} on port {1}. {2}", Name, Endpoint.Address.Uri.Port, exception);
                else
                    Logger.Error("Failed to create process for {0} on port {1}.", Name, Endpoint.Address.Uri.Port);

                return false;
            }

            _gridServerProcess = gridServerProcess;

            return IsOpened;
        }
    }

    /// <inheritdoc cref="IGridServerInstance.Unlock"/>
    public virtual void Unlock()
    {
        lock (_availableLock)
            _isAvailable = true;

        _availableWaitHandle.TrySetResult(true);
    }

    /// <inheritdoc cref="IGridServerInstance.Lock"/>
    public virtual void Lock()
    {
        lock (_availableLock)
            _isAvailable = false;

        // Reset the wait handle task.
        _availableWaitHandle = new TaskCompletionSource<bool>();
    }

    /// <inheritdoc cref="IGridServerInstance.LockAndTryStart"/>
    public void LockAndTryStart()
    {
        Lock();

        if (IsOpened) return;

        while (!TryStart())
            Thread.Sleep(1000);
    }

    /// <inheritdoc cref="IGridServerInstance.WaitForAvailable"/>
    public virtual bool WaitForAvailable(TimeSpan timeout)
    {
        if (IsAvailable) return true;

        var didRecieve = _availableWaitHandle.Task.Wait(timeout);

        return didRecieve && IsAvailable && _availableWaitHandle.Task.Result;
    }

    /// <inheritdoc cref="IGridServerInstance.TryStartNewProcess(bool)"/>
    public virtual bool TryStartNewProcess(bool force = false)
    {
        if (!force && _gridServerProcess != null)
            WaitForAvailable(TimeSpan.FromSeconds(10)); // make setting

        if (_gridServerProcess != null)
        {

            GridServerDeployer.KillProcess(_gridServerProcess, out var ex);
            if (ex is not null) return false;

            _gridServerProcess = null;
        }

        return TryStart();
    }

    #endregion |LifeCycle Managment Helpers|

    #region |Invocation Helpers|

    /// <summary>
    /// Invoke a method on the <see cref="IGridServerProcess"/> SOAP interface that does not expect a return value.
    /// </summary>
    /// <param name="method">The name of the SOAP method to invoke.</param>
    /// <param name="args">The arguments supplied to the SOAP method.</param>
    protected void InvokeSoap(string method, params object[] args) => InvokeSoap<VoidResult>(method, args);

    /// <summary>
    /// Invoke a method on the <see cref="IGridServerProcess"/> SOAP interface and return the result.
    /// 
    /// Virtual so inherited classes can override with custom implementations.
    /// 
    /// For overrides, expect to handle <see cref="VoidResult"/>.
    /// </summary>
    /// <typeparam name="T">The typeof the result.</typeparam>
    /// <param name="method">The name of the SOAP method to invoke.</param>
    /// <param name="args">The arguments supplied to the SOAP method.</param>
    /// <returns>The return type.</returns>
    /// <exception cref="TimeoutException">The SOAP method reached its max attempts to give a result.</exception>
    protected virtual T InvokeSoap<T>(string method, params object[] args)
    {
        try
        {
            PerformanceMonitor.TotalInvocations.Increment();

            LockAndTryStart();
            TryGetBaseMethodToInvoke(args, false, method, out var methodToInvoke);

            for (var i = 0; i < _maxAttemptsToCallSoap; i++)
            {
                var result = ActuallyInvoke<T>(methodToInvoke, method, i + 1, out var didFail, args);

                if (!didFail)
                    return result;
            }

            PerformanceMonitor.TotalInvocationsThatFailed.Increment();

            throw new TimeoutException($"The SOAP method '{method}' on '{this}' reached its max attempts to give a result.");
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>
    /// Invoke a method on the <see cref="IGridServerProcess"/> SOAP interface that does not expect a return value asynchronously.
    /// </summary>
    /// <param name="method">The name of the SOAP method to invoke.</param>
    /// <param name="args">The arguments supplied to the SOAP method.</param>
    /// <returns>An awaitable task.</returns>
    protected async Task InvokeSoapAsync(string method, params object[] args) => await InvokeSoapAsync<VoidResult>(method, args);

    /// <summary>
    /// Invoke a method on the <see cref="IGridServerProcess"/> SOAP interface and return the result asynchronously.
    /// 
    /// Virtual so inherited classes can override with custom implementations.
    /// 
    /// For overrides, expect to handle <see cref="VoidResult"/>.
    /// </summary>
    /// <typeparam name="T">The typeof the result.</typeparam>
    /// <param name="method">The name of the SOAP method to invoke.</param>
    /// <param name="args">The arguments supplied to the SOAP method.</param>
    /// <returns>The return type.</returns>
    /// <exception cref="TimeoutException">The SOAP method reached its max attempts to give a result.</exception>
    protected virtual async Task<T> InvokeSoapAsync<T>(string method, params object[] args)
    {
        try
        {
            LockAndTryStart();
            TryGetBaseMethodToInvoke(args, true, method, out var methodToInvoke);

            for (var i = 0; i < _maxAttemptsToCallSoap; i++)
            {
                var (didFail, result) = await ActuallyInvokeAsync<T>(methodToInvoke, method, i + 1, args);

                if (!didFail)
                    return result;
            }

            PerformanceMonitor.TotalInvocationsThatFailed.Increment();

            throw new TimeoutException($"The SOAP method '{method}' on '{this}' reached its max attempts to give a result.");
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>
    /// Handle an exception for the specified SOAP method.
    /// </summary>
    /// <param name="method">The name of the SOAP method that threw an exception.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="currentTry">The current attempt number.</param>
    protected virtual void HandleException(string method, Exception exception, int currentTry)
    {
        PerformanceMonitor.TotalInvocationsThatFailed.Increment();

        if (IsReasonForRecovery(exception))
        {
            // Make a new process and try again, otherwise just throw the exception
            Logger.Warning("Restarting process for '{0}' because of exception: {1}", this.Name, exception.Message);

            TryStartNewProcess();
        }

        if (exception is TargetInvocationException && exception.InnerException is FaultException)
        {
            // Re-throw the inner exception
            throw exception.InnerException;
        }

#if DEBUG || DEBUG_LOGGING_IN_PROD
        Logger.Error("Exception occurred when trying to execute SOAP method '{0}' on '{1}': {2}. Retrying...", method, this.Name, exception.InnerException.ToString());
#else
        Logger.Warning("Exception occurred when trying to execute SOAP method '{0}' on '{1}': {2}. Retrying...", method, this.Name, exception.InnerException.Message);
#endif
        return;
    }
    
    private void TryGetBaseMethodToInvoke(IEnumerable<object> args, bool isAsync, string lastMethod, out MethodInfo methodToInvoke)
    {
        methodToInvoke = typeof(ComputeCloudServiceSoapClient).GetMethod(
            lastMethod,
            BindingFlags.Instance | BindingFlags.Public,
            null,
            args.Select(x => x.GetType())
                .ToArray(),
            null
        );

        if (methodToInvoke == null)
            throw new ApplicationException($"Unknown grid server method '{lastMethod}'.");

        if (isAsync && methodToInvoke.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            throw new ApplicationException($"The method '{lastMethod}' is not async.");
    }

    private T ActuallyInvoke<T>(MethodInfo methodToInvoke, string lastMethod, int currentTry, out bool didFail, params object[] args)
    {
        didFail = true;

        try
        {
            var returnValue = methodToInvoke.Invoke(this, args);

            didFail = false;

            PerformanceMonitor.TotalInvocationsThatSucceeded.Increment();

            if (typeof(T) == typeof(VoidResult))
                return default(T);

            return (T)returnValue;
        }
        catch (Exception ex)
        {
            HandleException(lastMethod, ex, currentTry);

            return default(T);
        }
    }

    private async Task<(bool didFail, T result)> ActuallyInvokeAsync<T>(MethodInfo methodToInvoke, string lastMethod, int currentTry, params object[] args)
    {
        try
        {
            var returnValue = await ((Task<T>)methodToInvoke.Invoke(this, args)).ConfigureAwait(false);

            PerformanceMonitor.TotalInvocationsThatSucceeded.Increment();

            if (typeof(T) == typeof(VoidResult))
                return (true, default(T));

            return (true, returnValue);
        }
        catch (Exception ex)
        {
            HandleException(lastMethod, ex, currentTry);

            return (false, default(T));
        }
    }

    #endregion |Invocation Helpers|

    #region |Recovery Methods|

    private const string BatchJobTimedOutMessage = "BatchJob Timeout";
    private const string BatchJobAlreadyRunningMessage = "Cannot invoke BatchJob while another job is running";

    /// <summary>
    /// Determines if the specified exception is a reason for recovery.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the exception is a reason for recovery, false otherwise.</returns>
    /// <remarks>
    /// Default cases for recovery are:
    /// - EndpointNotFoundException
    /// - TimeoutException
    /// - FaultException with message "BatchJob Timeout" or "Cannot invoke BatchJob while another job is running"
    /// - CommunicationException with inner exception of type WebException
    /// </remarks>
    protected virtual bool IsReasonForRecovery(Exception exception)
    {
        if (exception is TargetInvocationException e)
        {
            switch (e.InnerException)
            {
                case EndpointNotFoundException:
                case TimeoutException:
                    return true;

                case FaultException ex:
                    var message = ex.Message;

                    switch (message)
                    {
                        case BatchJobTimedOutMessage:
                        case BatchJobAlreadyRunningMessage:
                            return true;
                    }

                    break;

                case CommunicationException ex:
                    return ex.InnerException is WebException;
            }
        }

        return false;
    }

    #endregion

    #region |SOAP Methods|

    /// <inheritdoc cref="IGridServerInstance.HelloWorld"/>
    public new string HelloWorld() => InvokeSoap<string>(nameof(HelloWorld));

    /// <inheritdoc cref="IGridServerInstance.HelloWorldAsync"/>
    public new async Task<string> HelloWorldAsync() => await InvokeSoapAsync<string>(nameof(HelloWorldAsync));


    /// <inheritdoc cref="IGridServerInstance.GetVersion"/>
    public new string GetVersion() => InvokeSoap<string>(nameof(GetVersion));

    /// <inheritdoc cref="IGridServerInstance.GetVersionAsync"/>
    public new async Task<string> GetVersionAsync() => await InvokeSoapAsync<string>(nameof(GetVersionAsync));


    /// <inheritdoc cref="IGridServerInstance.GetStatus"/>
    public new Status GetStatus() => InvokeSoap<Status>(nameof(GetStatus));

    /// <inheritdoc cref="IGridServerInstance.GetStatusAsync"/>
    public new async Task<Status> GetStatusAsync() => await InvokeSoapAsync<Status>(nameof(GetStatusAsync));


    /// <inheritdoc cref="IGridServerInstance.OpenJob(Job, ScriptExecution)"/>
    [Obsolete($"{nameof(OpenJob)} is deprecated, use {nameof(OpenJobEx)} instead.")]
    public new LuaValue[] OpenJob(Job job, ScriptExecution script) => InvokeSoap<LuaValue[]>(nameof(OpenJob), job, script);

    /// <inheritdoc cref="IGridServerInstance.OpenJobAsync(Job, ScriptExecution)"/>
    [Obsolete($"{nameof(OpenJobAsync)} is deprecated, use {nameof(OpenJobExAsync)} instead.")]
    public new async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script)
        => await InvokeSoapAsync<OpenJobResponse>(nameof(OpenJobAsync), job, script);


    /// <inheritdoc cref="IGridServerInstance.OpenJobEx(Job, ScriptExecution)"/>
    public new LuaValue[] OpenJobEx(Job job, ScriptExecution script) => InvokeSoap<LuaValue[]>(nameof(OpenJobEx), job, script);

    /// <inheritdoc cref="IGridServerInstance.OpenJobExAsync(Job, ScriptExecution)"/>
    public new async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script)
        => await InvokeSoapAsync<LuaValue[]>(nameof(OpenJobExAsync), job, script);


    /// <inheritdoc cref="IGridServerInstance.RenewLease(string, double)"/>
    public new double RenewLease(string jobId, double expirationInSeconds) => InvokeSoap<double>(nameof(RenewLease), jobId, expirationInSeconds);

    /// <inheritdoc cref="IGridServerInstance.RenewLeaseAsync(string, double)"/>
    public new async Task<double> RenewLeaseAsync(string jobId, double expirationInSeconds)
        => await InvokeSoapAsync<double>(nameof(RenewLeaseAsync), jobId, expirationInSeconds);


    /// <inheritdoc cref="IGridServerInstance.Execute(string, ScriptExecution)"/>
    [Obsolete($"{nameof(Execute)} is deprecated, use {nameof(ExecuteEx)} instead.")]
    public new LuaValue[] Execute(string jobId, ScriptExecution script) => InvokeSoap<LuaValue[]>(nameof(Execute), jobId, script);

    /// <inheritdoc cref="IGridServerInstance.ExecuteAsync(string, ScriptExecution)"/>
    [Obsolete($"{nameof(ExecuteAsync)} is deprecated, use {nameof(ExecuteExAsync)} instead.")]
    public new async Task<ExecuteResponse> ExecuteAsync(string jobId, ScriptExecution script)
        => await InvokeSoapAsync<ExecuteResponse>(nameof(ExecuteAsync), jobId, script);


    /// <inheritdoc cref="IGridServerInstance.ExecuteEx(string, ScriptExecution)"/>
    public new LuaValue[] ExecuteEx(string jobId, ScriptExecution script) => InvokeSoap<LuaValue[]>(nameof(ExecuteEx), jobId, script);

    /// <inheritdoc cref="IGridServerInstance.ExecuteExAsync(string, ScriptExecution)"/>
    public new async Task<LuaValue[]> ExecuteExAsync(string jobId, ScriptExecution script)
        => await InvokeSoapAsync<LuaValue[]>(nameof(ExecuteExAsync), jobId, script);


    /// <inheritdoc cref="IGridServerInstance.CloseJob(string)"/>
    public new void CloseJob(string jobId) => InvokeSoap(nameof(CloseJob), jobId);

    /// <inheritdoc cref="IGridServerInstance.CloseJobAsync(string)"/>
    public new async Task CloseJobAsync(string jobId) => await InvokeSoapAsync(nameof(CloseJobAsync), jobId);


    /// <inheritdoc cref="IGridServerInstance.BatchJob(Job, ScriptExecution)"/>
    [Obsolete($"{nameof(BatchJob)} is deprecated, use {nameof(BatchJobEx)} instead.")]
    public new LuaValue[] BatchJob(Job job, ScriptExecution script) => InvokeSoap<LuaValue[]>(nameof(BatchJob), job, script);

    /// <inheritdoc cref="IGridServerInstance.BatchJobAsync(Job, ScriptExecution)"/>
    [Obsolete($"{nameof(BatchJobAsync)} is deprecated, use {nameof(BatchJobExAsync)} instead.")]
    public new async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script)
        => await InvokeSoapAsync<BatchJobResponse>(nameof(BatchJobAsync), job, script);


    /// <inheritdoc cref="IGridServerInstance.BatchJobEx(Job, ScriptExecution)"/>
    public new LuaValue[] BatchJobEx(Job job, ScriptExecution script) => InvokeSoap<LuaValue[]>(nameof(BatchJobEx), job, script);

    /// <inheritdoc cref="IGridServerInstance.BatchJobExAsync(Job, ScriptExecution)"/>
    public new async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script)
        => await InvokeSoapAsync<LuaValue[]>(nameof(BatchJobExAsync), job, script);


    /// <inheritdoc cref="IGridServerInstance.GetExpiration(string)"/>
    public new double GetExpiration(string jobId) => InvokeSoap<double>(nameof(GetExpiration), jobId);

    /// <inheritdoc cref="IGridServerInstance.GetExpirationAsync(string)"/>
    public new async Task<double> GetExpirationAsync(string jobId) => await InvokeSoapAsync<double>(nameof(GetExpirationAsync), jobId);


    /// <inheritdoc cref="IGridServerInstance.GetAllJobs"/>
    [Obsolete($"{nameof(GetAllJobs)} is deprecated, use {nameof(GetAllJobsEx)} instead.")]
    public new Job[] GetAllJobs() => InvokeSoap<Job[]>(nameof(GetAllJobs));

    /// <inheritdoc cref="IGridServerInstance.GetAllJobsAsync"/>
    [Obsolete($"{nameof(GetAllJobsAsync)} is deprecated, use {nameof(GetAllJobsExAsync)} instead.")]
    public new async Task<GetAllJobsResponse> GetAllJobsAsync() => await InvokeSoapAsync<GetAllJobsResponse>(nameof(GetAllJobsAsync));


    /// <inheritdoc cref="IGridServerInstance.GetAllJobsEx"/>
    public new Job[] GetAllJobsEx() => InvokeSoap<Job[]>(nameof(GetAllJobsEx));

    /// <inheritdoc cref="IGridServerInstance.GetAllJobsExAsync"/>
    public new async Task<Job[]> GetAllJobsExAsync() => await InvokeSoapAsync<Job[]>(nameof(GetAllJobsExAsync));


    /// <inheritdoc cref="IGridServerInstance.CloseExpiredJobs"/>
    public new int CloseExpiredJobs() => InvokeSoap<int>(nameof(CloseExpiredJobs));

    /// <inheritdoc cref="IGridServerInstance.CloseExpiredJobsAsync"/>
    public new async Task<int> CloseExpiredJobsAsync() => await InvokeSoapAsync<int>(nameof(CloseExpiredJobsAsync));


    /// <inheritdoc cref="IGridServerInstance.CloseAllJobs"/>
    public new int CloseAllJobs() => InvokeSoap<int>(nameof(CloseAllJobs));

    /// <inheritdoc cref="IGridServerInstance.CloseAllJobsAsync"/>
    public new async Task<int> CloseAllJobsAsync() => await InvokeSoapAsync<int>(nameof(CloseAllJobsAsync));


    /// <inheritdoc cref="IGridServerInstance.Diag(int, string)"/>
    public new LuaValue[] Diag(int type, string jobId) => InvokeSoap<LuaValue[]>(nameof(Diag), type, jobId);

    /// <inheritdoc cref="IGridServerInstance.DiagAsync(int, string)"/>
    public new async Task<DiagResponse> DiagAsync(int type, string jobId) => await InvokeSoapAsync<DiagResponse>(nameof(DiagAsync), type, jobId);


    /// <inheritdoc cref="IGridServerInstance.DiagEx(int, string)"/>
    public new LuaValue[] DiagEx(int type, string jobId) => InvokeSoap<LuaValue[]>(nameof(DiagEx), type, jobId);

    /// <inheritdoc cref="IGridServerInstance.DiagExAsync(int, string)"/>
    public new async Task<LuaValue[]> DiagExAsync(int type, string jobId) => await InvokeSoapAsync<LuaValue[]>(nameof(DiagExAsync), type, jobId);

    #endregion |SOAP Methods|

    #region Auto-Generated Items

    /// <inheritdoc cref="object.ToString"/>
    public override string ToString()
        => string.Format(
                 "[{0}] [{1}] Instance [{2}], Name = {3}, State = {4}",
                 _isPersistent ? "Persistent" : "Disposable",
                 _isPoolable ? "Poolable" : "Non Poolable",
                 Endpoint.Address,
                 Name,
                 IsOpened ? "Open" : "Closed"
             );

    /// <inheritdoc cref="object.Equals(object)"/>
    public override bool Equals(object obj) => obj is GridServerInstance instance &&
                                               _maxAttemptsToCallSoap ==
                                               instance._maxAttemptsToCallSoap &&
                                               _isPersistent == instance._isPersistent &&
                                               _name == instance._name;

    /// <inheritdoc cref="object.GetHashCode"/>
    public override int GetHashCode()
    {
        int hashCode = -638914433;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_name);
        hashCode = hashCode * -1521134295 + _isPoolable.GetHashCode();
        hashCode = hashCode * -1521134295 + _isPersistent.GetHashCode();
        hashCode = hashCode * -1521134295 + _maxAttemptsToCallSoap.GetHashCode();
        hashCode = hashCode * -1521134295 + _isAvailable.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<IGridServerProcess>.Default.GetHashCode(_gridServerProcess);
        return hashCode;
    }

    /// <summary>
    /// Does <paramref name="left"/> equal <paramref name="right"/>?
    /// </summary>
    /// <param name="left">The left hand side.</param>
    /// <param name="right">The right hand side.</param>
    /// <returns>True if <paramref name="left"/> equals <paramref name="right"/>.</returns>
    public static bool operator ==(GridServerInstance left, GridServerInstance right) => left?.GetHashCode() == right?.GetHashCode();

    /// <summary>
    /// Does <paramref name="left"/> not equal <paramref name="right"/>?
    /// </summary>
    /// <param name="left">The left hand side.</param>
    /// <param name="right">The right hand side.</param>
    /// <returns>True if <paramref name="left"/> does not equal <paramref name="right"/>.</returns>
    public static bool operator !=(GridServerInstance left, GridServerInstance right) => left?.GetHashCode() != right?.GetHashCode();

    #endregion Auto-Generated Items
}
