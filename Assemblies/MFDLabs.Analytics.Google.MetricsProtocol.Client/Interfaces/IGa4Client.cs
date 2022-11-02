#nullable enable

namespace MFDLabs.Analytics.Google.MetricsProtocol.Client;

using System.Threading;
using System.Threading.Tasks;

public interface IGa4Client
{
    /// <summary>
    /// Sends an event to Google Analytics.
    ///
    /// As of now, Metrics Protocol only supports sending events.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="params">The event parameters.</param>
    /// <param name="properties">The user properties.</param>
    void FireEvent(
        string clientId,
        string eventName,
        object? @params,
        object? properties
    );

    /// <summary>
    /// Sends an event to Google Analytics.
    ///
    /// As of now, Metrics Protocol only supports sending events.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="params">The event parameters.</param>
    /// <param name="properties">The user properties.</param>
    /// <returns>An awaitable task</returns>
    Task FireEventAsync(
        string clientId,
        string eventName,
        CancellationToken? cancellationToken,
        object? @params,
        object? properties
    );
}
