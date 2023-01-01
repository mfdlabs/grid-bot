namespace MFDLabs.Grid;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for the grid health check clients.
/// </summary>
public interface IHealthCheckClient
{
    /// <summary>
    /// Gets or sets the health check path.
    /// </summary>
    string HealthCheckPath { get; set; }

    /// <summary>
    /// Checks the health of the target server.
    /// </summary>
    /// <returns>Health check status.</returns>
    HealthCheckStatus CheckHealth();

    /// <summary>
    /// Checks the health of the target server.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to use for the request.</param>
    /// <returns>Health check status.</returns>
    Task<HealthCheckStatus> CheckHealthAsync(CancellationToken? cancellationToken);
}
