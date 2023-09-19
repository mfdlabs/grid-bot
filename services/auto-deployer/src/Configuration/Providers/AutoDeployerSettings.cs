namespace Grid.AutoDeployer;

using System;

using Logging;

/// <summary>
/// Settings provider for settings used by the Roblox Users API.
/// </summary>
public class AutoDeployerSettings : BaseSettingsProvider<AutoDeployerSettings>
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.AutoDeployerPath;

    /// <summary>
    /// Gets the GHE url.
    /// </summary>
    public string GithubEnterpriseUrl => GetOrDefault(
        nameof(GithubEnterpriseUrl),
        string.Empty
    );

    /// <summary>
    /// Gets the registry subkey for versioning.
    /// </summary>
    public string VersioningRegistrySubKey => GetOrDefault(
        nameof(VersioningRegistrySubKey),
        string.Empty
    );

    /// <summary>
    /// Gets the name of the versioning registry key.
    /// </summary>
    public string VersioningRegistryVersionKeyName => GetOrDefault(
        nameof(VersioningRegistryVersionKeyName),
        "AppVersion"
    );

    /// <summary>
    /// Create the deployment path if it doesn't exist already?
    /// </summary>
    public bool CreateDeploymentPathIfNotExists => GetOrDefault(
        nameof(CreateDeploymentPathIfNotExists),
        true
    );

    /// <summary>
    /// Gets the GitHub token.
    /// </summary>
    public string GithubToken => GetOrDefault(
        nameof(GithubToken),
        string.Empty
    );

    /// <summary>
    /// Gets the deployment path.
    /// </summary>
    public string DeploymentPath => GetOrDefault(
        nameof(DeploymentPath),
        string.Empty
    );

    /// <summary>
    /// Gets the GitHub account or organization name.
    /// </summary>
    public string GithubAccountOrOrganizationName => GetOrDefault(
        nameof(GithubAccountOrOrganizationName),
        string.Empty
    );

    /// <summary>
    /// Gets the GitHub repository name.
    /// </summary>
    public string GithubRepositoryName => GetOrDefault(
        nameof(GithubRepositoryName),
        string.Empty
    );

    /// <summary>
    /// Gets the name of primary executable to be deployed.
    /// </summary>
    public string DeploymentPrimaryExecutableName => GetOrDefault(
        nameof(DeploymentPrimaryExecutableName),
        string.Empty
    );

    /// <summary>
    /// Gets the polling interval.
    /// </summary>
    public TimeSpan PollingInterval => GetOrDefault(
        nameof(PollingInterval),
        TimeSpan.FromSeconds(45)
    );

    /// <summary>
    /// Gets the name of the app that is being deployed.
    /// </summary>
    public string DeploymentAppName => GetOrDefault(
        nameof(DeploymentAppName),
        string.Empty
    );

    /// <summary>
    /// Gets the interval for the skipped versions invalidation.
    /// </summary>
    public TimeSpan SkippedVersionInvalidationInterval => GetOrDefault(
        nameof(SkippedVersionInvalidationInterval),
        TimeSpan.FromMinutes(5)
    );

    /// <summary>
    /// Should this auto deployer only deploy pre-releases?
    /// </summary>
    public bool OnlyDeployPreRelease => GetOrDefault(
        nameof(OnlyDeployPreRelease),
        false
    );

    /// <summary>
    /// Gets the max amount of attempts for the process watch dog to
    /// check if the process started.
    /// </summary>
    public int WatchDogMaxAttempts => GetOrDefault(
        nameof(WatchDogMaxAttempts),
        5
    );

    /// <summary>
    /// Gets the max wait time for the process watch dog to check
    /// if the process started.
    /// </summary>
    public TimeSpan WatchDogWaitTime => GetOrDefault(
        nameof(WatchDogWaitTime),
        TimeSpan.FromSeconds(5)
    );

    /// <summary>
    /// Gets the name of the environment logger.
    /// </summary>
    public string EnvironmentLoggerName => GetOrDefault(
        nameof(EnvironmentLoggerName),
        "grid-auto-deployer"
    );

    /// <summary>
    /// Gets the name of the environment logger.
    /// </summary>
    public LogLevel EnvironmentLogLevel => GetOrDefault(
        nameof(EnvironmentLogLevel),
        LogLevel.Information
    );
}
