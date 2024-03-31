namespace Grid.Bot;

using System;

internal static class SettingsProvidersDefaults
{
    private const string _vaultMountEnvVar = "VAULT_MOUNT";
    private const string _providerEnvironmentNameEnvVar = "ENVIRONMENT";
    private const string _defaultEnvironmentName = "development";
    private static string _providerEnvironmentName = Environment.GetEnvironmentVariable(_providerEnvironmentNameEnvVar) ?? _defaultEnvironmentName;

    public static string DiscordPath => $"{_providerEnvironmentName}/discord";
    public static string DiscordRolesPath => $"{_providerEnvironmentName}/discord-roles";
    public static string AvatarPath => $"{_providerEnvironmentName}/avatar";
    public static string GridPath => $"{_providerEnvironmentName}/grid";
    public static string BacktracePath => $"{_providerEnvironmentName}/backtrace";
    public static string MaintenancePath => $"{_providerEnvironmentName}/maintenance";
    public static string CommandsPath => $"{_providerEnvironmentName}/commands";
    public static string FloodCheckerPath => $"{_providerEnvironmentName}/floodcheckers";
    public static string ConsulPath => $"{_providerEnvironmentName}/consul";
    public static string UsersClientPath => $"{_providerEnvironmentName}/users-client";
    public static string ScriptsPath => $"{_providerEnvironmentName}/scripts";
    public static string ClientSettingsClientPath => $"{_providerEnvironmentName}/client-settings-client";
    public static string GlobalPath => $"{_providerEnvironmentName}/global";

    public const string DefaultMountPath = "grid-bot-settings";
    public static string MountPath = Environment.GetEnvironmentVariable(_vaultMountEnvVar) ?? DefaultMountPath;
}
