﻿namespace MFDLabs.Grid;

using System;
using System.IO;

using CliWrap;

using Logging;
using MFDLabs.Instrumentation;

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

        var command = Cli.Wrap("npm");

        if (_buildBeforeRun)
            command = command.WithArguments("run Build-And-Run");
        else
            command = command.WithArguments("start");

        command = command.WithWorkingDirectory(_webServerPath);

        command.ExecuteAsync();
    }

    private void CheckWorkspace()
    {
        _logger.Info("Checking the existance of the web server at '{0}'", _webServerPath);

        if (!Directory.Exists(_webServerPath))
        {
            _logger.Error("Unable to launch the web server because it could not be found at the path: '{0}'", _webServerPath);
            throw new InvalidOperationException(Properties.Resources.CouldNotFindWebServer);
        }
    }

    /// <inheritdoc cref="IWebServerDeployer.LaunchWebServer(int)"/>
    public void LaunchWebServer(int maxAttempts = 15)
    {
        var status = _healthCheckClient.CheckHealth();

        if (status == HealthCheckStatus.Failure)
            throw new InvalidOperationException(Properties.Resources.WebServerUpButBadHealthCheckText);
        
        if (status == HealthCheckStatus.Success)
            return;

        _logger.Info("Trying to launch web server...");

        CheckWorkspace();

        for (int attempt = 0; attempt < maxAttempts; ++attempt)
        {
            _logger.Info("Trying to contact web server at attempt No. {0}", attempt);

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

        _logger.Info("Trying to launch web server...");

        CheckWorkspace();

        for (int attempt = 0; attempt < maxAttempts; ++attempt)
        {
            _logger.Info("Trying to contact web server at attempt No. {0}", attempt);

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