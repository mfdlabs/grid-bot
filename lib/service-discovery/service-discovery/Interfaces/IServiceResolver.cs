namespace ServiceDiscovery;

using System.Net;
using System.ComponentModel;
using System.Collections.Generic;

/// <summary>
/// Resolver for fetching service endpoints.
/// </summary>
public interface IServiceResolver : INotifyPropertyChanged
{
    /// <summary>
    /// The IP endpoints for the service.
    /// </summary>
    ISet<IPEndPoint> EndPoints { get; }
}
