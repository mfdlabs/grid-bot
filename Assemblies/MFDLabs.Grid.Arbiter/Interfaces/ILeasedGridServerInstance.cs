/*
    File name: ILeasedGridServerInstance.cs
    Written By: @networking-owk
    Description: Represents the leased interface for a grid server instance to be consumed by the arbiter.

    Copyright MFDLABS 2001-2022. All rights reserved.
*/

namespace MFDLabs.Grid;

using System;

/// <summary>
/// Delegate for the <see cref="ILeasedGridServerInstance"/> to be notified when the lease has expired.
/// </summary>
/// <param name="instance">The instance that has expired.</param>
public delegate void OnExpired(ILeasedGridServerInstance instance);

/// <summary>
/// Represents the leased class for a grid server instance to be consumed by the arbiter.
/// </summary>
public interface ILeasedGridServerInstance : IGridServerInstance, IDisposable
{
    /// <summary>
    /// Get the expiration of the lease.
    /// </summary>
    DateTime Expiration { get; }

    /// <summary>
    /// Get the lease duration.
    /// </summary>
    TimeSpan Lease { get; }

    /// <summary>
    /// Does the instance have a lease?
    /// </summary>
    bool HasLease { get; }

    /// <summary>
    /// Is the lease expired?
    /// </summary>
    bool IsExpired { get; }


    /// <summary>
    /// Renew the instance lease.
    /// </summary>
    void RenewLease();

    /// <summary>
    /// Subscribe to the lease expiration event.
    /// </summary>
    /// <param name="delegate">The delegate to be called when the lease expires.</param>
    void SubscribeExpirationListener(OnExpired @delegate);

    /// <summary>
    /// Unsubscribe from the lease expiration event.
    /// </summary>
    /// <param name="delegate">The delegate to be called when the lease expires.</param>
    void UnsubscribeExpirationListener(OnExpired @delegate);
}
