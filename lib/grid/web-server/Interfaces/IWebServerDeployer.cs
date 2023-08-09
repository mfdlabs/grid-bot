namespace Grid;

using System.Diagnostics;

/// <summary>
/// Base interface for launching web servers.
/// </summary>
public interface IWebServerDeployer
{
    /// <summary>
    /// Get the process that is running the web server.
    /// </summary>
    Process Process { get; }

    /// <summary>
    /// Stop the web server.
    /// </summary>
    void StopWebServer();

    /// <summary>
    /// Launch the web server.
    /// </summary>
    /// <param name="maxAttempts">Max attempts.</param>
    /// <exception cref="System.TimeoutException">The grid deployer exceeded it's maximum attempts when trying to launch the web server.</exception>
    /// <exception cref="System.InvalidOperationException">The web server was launched but the health check text was incorrect.</exception>
    void LaunchWebServer(int maxAttempts = 15);

    /// <summary>
    /// Launch the web server but with a status code instead of exceptions.
    /// </summary>
    /// <param name="maxAttempts">Max attempts.</param>
    /// <returns>The deployment status.</returns>
    WebServerDeploymentStatus LaunchWebServerSafe(int maxAttempts = 15);
}
