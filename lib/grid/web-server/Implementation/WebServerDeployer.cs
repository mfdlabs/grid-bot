namespace Grid;

using System;
using System.IO;
using System.Diagnostics;

using Logging;

/// <summary>
/// Deployment status for the Web Server.
/// </summary>
public enum WebServerDeploymentStatus
{
    /// <summary>
    /// Max attempts exceeded to open the web server.
    /// </summary>
    MaxAttemptsExceeded,

    /// <summary>
    /// Web server responded but with incorrect health check text.
    /// </summary>
    UpButIncorrectHealthCheckText,

    /// <summary>
    /// Web server was successfully launched.
    /// </summary>
    Success
}

/// <inheritdoc cref="IWebServerDeployer"/>
public class WebServerDeployer : IWebServerDeployer
{
    private readonly bool _buildBeforeRun;
    private readonly string _webServerPath;
    private readonly ILogger _logger;
    private readonly IHealthCheckClient _healthCheckClient;
    
    private Process _process;
    private bool _runningWebServerLaunch = false;

    /// <summary>
    /// Construct a new instance of <see cref="WebServerDeployer"/>
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="healthCheckClient">The health check client to use.</param>
    /// <param name="webServerPath">Path to the web server.</param>
    /// <param name="buildBeforeRun">Should we build the web server before running it?</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="healthCheckClient"/> cannot be null.
    /// - <paramref name="webServerPath"/> cannot be null.
    /// </exception>
    public WebServerDeployer(
        ILogger logger,
        IHealthCheckClient healthCheckClient,
        string webServerPath,
        bool buildBeforeRun = false
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthCheckClient = healthCheckClient ?? throw new ArgumentNullException(nameof(healthCheckClient));
        _webServerPath = webServerPath ?? throw new ArgumentNullException(nameof(webServerPath));
        
        _buildBeforeRun = buildBeforeRun;

        CheckWorkspace();
    }

    private void InvokeDeploymentOnWebServer()
    {
        if (_runningWebServerLaunch) return;

        _runningWebServerLaunch = true;

        var startInfo = new ProcessStartInfo
        {
            FileName = "npm",
            UseShellExecute = true,
            CreateNoWindow = true,
            WorkingDirectory = _webServerPath,
            WindowStyle = ProcessWindowStyle.Maximized
        };

        if (_buildBeforeRun)
            startInfo.Arguments = "run Build-And-Run";
        else
            startInfo.Arguments = "start";

        _process = Process.Start(startInfo);
    }

    private void CheckWorkspace()
    {
        _logger.Information("Checking the existance of the web server at '{0}'", _webServerPath);

        if (!Directory.Exists(_webServerPath))
        {
            _logger.Error("Unable to launch the web server because it could not be found at the path: '{0}'", _webServerPath);
            throw new InvalidOperationException(Properties.Resources.CouldNotFindWebServer);
        }
    }

    /// <inheritdoc cref="IWebServerDeployer.Process"/>
    public Process Process => _process;

    /// <inheritdoc cref="IWebServerDeployer.StopWebServer"/>
    public void StopWebServer()
    {
        _logger.Information("Stopping the web server");

        if (_process == null)
        {
            _logger.Warning("The web server was not running");
            return;
        }

        try
        {
            _process.Kill();
            _process.Dispose();

            _process = null;
        }
        catch (InvalidOperationException)
        {
            _process = null;
        }

        _logger.Information("Web server stopped");
    }

    /// <inheritdoc cref="IWebServerDeployer.LaunchWebServer(int)"/>
    public void LaunchWebServer(int maxAttempts = 15)
    {
        if (_process != null && _process.HasExited)
            _process = null; // Case for when the process was killed externally

        var status = _healthCheckClient.CheckHealth();

        if (status == HealthCheckStatus.Failure)
            throw new InvalidOperationException(Properties.Resources.WebServerUpButBadHealthCheckText);
        
        if (status == HealthCheckStatus.Success)
            return;

        _logger.Information("Trying to launch web server...");

        CheckWorkspace();

        for (int attempt = 0; attempt < maxAttempts; ++attempt)
        {
            _logger.Information("Trying to contact web server at attempt No. {0}", attempt);

            status = _healthCheckClient.CheckHealth();

            if (status == HealthCheckStatus.Failure)
                throw new InvalidOperationException(Properties.Resources.WebServerUpButBadHealthCheckText);
            
            if (status == HealthCheckStatus.Success)
            {
                _runningWebServerLaunch = false;

                return;
            }

            InvokeDeploymentOnWebServer();
        }

        throw new TimeoutException(Properties.Resources.MaxAttemptsExceededWhenLaunchingWebServer);
    }

    /// <inheritdoc cref="IWebServerDeployer.LaunchWebServerSafe(int)"/>
    public WebServerDeploymentStatus LaunchWebServerSafe(int maxAttempts = 15)
    {
        var status = _healthCheckClient.CheckHealth();

        if (status == HealthCheckStatus.Failure) return WebServerDeploymentStatus.UpButIncorrectHealthCheckText;
        if (status == HealthCheckStatus.Success) return WebServerDeploymentStatus.Success;

        _logger.Information("Trying to launch web server...");

        CheckWorkspace();

        for (int attempt = 0; attempt < maxAttempts; ++attempt)
        {
            _logger.Information("Trying to contact web server at attempt No. {0}", attempt);

            status = _healthCheckClient.CheckHealth();

            if (status == HealthCheckStatus.Failure) return WebServerDeploymentStatus.UpButIncorrectHealthCheckText;
            if (status == HealthCheckStatus.Success)
            {
                _runningWebServerLaunch = false;

                return WebServerDeploymentStatus.Success;
            }

            InvokeDeploymentOnWebServer();
        }

        return WebServerDeploymentStatus.MaxAttemptsExceeded;
    }
}
