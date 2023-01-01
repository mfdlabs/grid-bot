/*
    File name: LeasedGridServerInstance.cs
    Written By: @networking-owk
    Description: Represents the leased class for a grid server instance to be consumed by the arbiter.

    Copyright MFDLABS 2001-2022. All rights reserved.
*/



namespace MFDLabs.Grid;

using System;
using System.Threading.Tasks;
using System.ServiceModel.Channels;

using Logging;
using Instrumentation;
using System.ServiceModel;

/// <summary>
/// Represents the leased class for a grid server instance to be consumed by the arbiter.
/// </summary>
public class LeasedGridServerInstance : GridServerInstance, ILeasedGridServerInstance, IGridServerInstance, IDisposable
{
    #region |Private Members|

    private static readonly IRandom _random = RandomFactory.GetDefaultRandom();

    private readonly TimeSpan _lease;
    private readonly object _expirationCheckLock = new();

    private OnExpired _onExpiredListeners;
    private DateTime _expiration;

    #endregion |Private Members|

    #region |Informative Members|

    /// <summary>
    /// Get the default lease time for a grid server instance.
    /// </summary>
    public static TimeSpan DefaultLease => global::MFDLabs.Grid.Properties.Settings.Default.DefaultLeasedGridServerInstanceLease;

    /// <inheritdoc cref="ILeasedGridServerInstance.Expiration"/>
    public DateTime Expiration => _expiration;

    /// <inheritdoc cref="ILeasedGridServerInstance.Lease"/>
    public TimeSpan Lease => _lease;

    /// <inheritdoc cref="ILeasedGridServerInstance.HasLease"/>
    public bool HasLease => _lease != TimeSpan.Zero;

    /// <inheritdoc cref="ILeasedGridServerInstance.IsExpired"/>
    public bool IsExpired => _expiration.Subtract(DateTime.Now) <= TimeSpan.Zero;

    /// <inheritdoc cref="IGridServerInstance.IsAvailable"/>
    public new bool IsAvailable => base.IsAvailable && !IsExpired;

    #endregion |Informative Members|

    #region |Constructors|

    /// <summary>
    /// Construct a new instance of <see cref="LeasedGridServerInstance"/>.
    /// </summary>
    /// <param name="lease">The lease time for the grid server instance.</param>
    /// <param name="counterRegistry">The <see cref="ICounterRegistry"/> to use for instrumentation.</param>
    /// <param name="logger">The <see cref="ILogger"/> for debug logging.</param>
    /// <param name="gridServerArbiter">The <see cref="IGridServerArbiter"/> that created this instance.</param>
    /// <param name="gridServerDeployer">The <see cref="IGridServerDeployer"/> for managing the <see cref="IGridServerProcess"/>.</param>
    /// <param name="httpBinding">The <see cref="Binding"/> given to <see cref="IGridServerArbiter"/> to interact with WCF.</param>
    /// <param name="remoteAddress">The remote address of the instance.</param>
    /// <param name="name">The name of the instance.</param>
    /// <param name="maxAttemptsToCallSoap">The maximum attempts this instance can initiate a failing SOAP call before it throws a <see cref="TimeoutException"/>.</param>
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
    internal LeasedGridServerInstance(
        TimeSpan lease,
        ICounterRegistry counterRegistry,
        ILogger logger,
        IGridServerArbiter gridServerArbiter,
        IGridServerDeployer gridServerDeployer,
        Binding httpBinding,
        EndpointAddress remoteAddress,
        string name,
        int maxAttemptsToCallSoap = 5,
        bool isPoolable = true,
        bool startNow = true,
        IGridServerProcess gridServerProcess = null // Optional for Arbiter.Discover()
    )
        : base(
            counterRegistry,
            logger,
            gridServerArbiter,
            gridServerDeployer,
            httpBinding,
            remoteAddress,
            name,
            maxAttemptsToCallSoap,
            false,
            isPoolable,
            startNow,
            gridServerProcess
        )
    {
        _lease = lease;
        _expiration = DateTime.Now.Add(lease);
    }

    #endregion |Constructors|

    #region |Leasing Helpers|

    /// <inheritdoc cref="ILeasedGridServerInstance.RenewLease"/>
    public void RenewLease()
    {
        if (IsDisposed)
            return;

        lock (_expirationCheckLock)
        {
            Logger.LifecycleEvent("Renewing instance '{0}' lease '{1}', current expiration '{2}'", Name, Lease, Expiration);

            _expiration = DateTime.Now.Add(_lease);

            Logger.LifecycleEvent("New expiration for instance '{0}' is '{1}'", Name, Expiration);

            ScheduleExpirationCheck();
        }
    }

    /// <inheritdoc cref="ILeasedGridServerInstance.SubscribeExpirationListener(OnExpired)"/>
    public void SubscribeExpirationListener(OnExpired @delegate)
    {
        if (IsDisposed)
            return;

        lock (_expirationCheckLock)
        {
            Logger.Warning("Subscribing expiration listener '{0}.{1}'", @delegate.Method.DeclaringType.FullName, @delegate.Method.Name);
            
            _onExpiredListeners += @delegate;
        }
    }

    /// <inheritdoc cref="ILeasedGridServerInstance.UnsubscribeExpirationListener(OnExpired)"/>
    public void UnsubscribeExpirationListener(OnExpired @delegate)
    {
        if (IsDisposed)
            return;

        lock (_expirationCheckLock)
        {
            Logger.Warning("Unsubscribing expiration listener '{0}.{1}'", @delegate.Method.DeclaringType.FullName, @delegate.Method.Name);

            _onExpiredListeners -= @delegate;
        }
    }
    
    private void ScheduleExpirationCheck()
    {
        if (IsDisposed)
            return;

        var span = TimeSpan.FromMilliseconds((1 + 0.2 * _random.NextDouble()) * _lease.TotalMilliseconds + 20);

        Task.Run(async () =>
        {
            await Task.Delay(span);

            CheckExpiration();
        });
    }

    private void CheckExpiration()
    {
        if (IsDisposed)
            return;

        lock (_expirationCheckLock)
        {
            if (IsExpired)
            {
                Logger.Warning("Instance '{0}' lease has expired, disposing...", Name);
                Dispose();
            }
            else
                ScheduleExpirationCheck();
        }
    }

    #endregion |Leasing Helpers|

    #region |Overrides|

    /// <inheritdoc cref="IGridServerInstance.Unlock"/>
    public override void Unlock()
    {
        base.Unlock();
        RenewLease();
    }

    /// <inheritdoc cref="object.ToString"/>
    public override string ToString() 
        => string.Format("[Leased] {0}, Lease = {1}, Expiration = {2}", base.ToString(), Lease, Expiration);

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public new void Dispose()
    {
        if (IsDisposed) return;

        lock (_expirationCheckLock)
        {
            GC.SuppressFinalize(this);

            _onExpiredListeners?.Invoke(this);
            base.Dispose();
        }
    }

    #endregion |Overrides|
}
