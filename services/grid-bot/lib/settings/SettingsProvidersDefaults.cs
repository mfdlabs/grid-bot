namespace Grid.Bot;

using System;

internal static class SettingsProvidersDefaults
{
    private const string VaultMountEnvVar = "VAULT_MOUNT";
    private const string DefaultMountPath = "grid-bot-settings";
    
    public static string DiscordPath => $"{EnvironmentProvider.EnvironmentName}/discord";
    public static string DiscordRolesPath => $"{EnvironmentProvider.EnvironmentName}/discord-roles";
    public static string AvatarPath => $"{EnvironmentProvider.EnvironmentName}/avatar";
    public static string GridPath => $"{EnvironmentProvider.EnvironmentName}/grid";
    public static string BacktracePath => $"{EnvironmentProvider.EnvironmentName}/backtrace";
    public static string MaintenancePath => $"{EnvironmentProvider.EnvironmentName}/maintenance";
    public static string CommandsPath => $"{EnvironmentProvider.EnvironmentName}/commands";
    public static string FloodCheckerPath => $"{EnvironmentProvider.EnvironmentName}/floodcheckers";
    public static string ConsulPath => $"{EnvironmentProvider.EnvironmentName}/consul";
    public static string UsersClientPath => $"{EnvironmentProvider.EnvironmentName}/users-client";
    public static string ScriptsPath => $"{EnvironmentProvider.EnvironmentName}/scripts";
    public static string ClientSettingsPath => $"{EnvironmentProvider.EnvironmentName}/client-settings";
    public static string GlobalPath => $"{EnvironmentProvider.EnvironmentName}/global";
    public static string WebPath => $"{EnvironmentProvider.EnvironmentName}/web";
    public static string GrpcPath => $"{EnvironmentProvider.EnvironmentName}/grpc";

    public static readonly string MountPath = Environment.GetEnvironmentVariable(VaultMountEnvVar) ?? DefaultMountPath;
}
