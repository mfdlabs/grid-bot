namespace Grid.Bot;

using System;

internal static class SettingsProvidersDefaults
{
    public static string DiscordPath => $"{EnvironmentDataProvider.EnvironmentName}/discord";
    public static string DiscordRolesPath => $"{EnvironmentDataProvider.EnvironmentName}/discord-roles";
    public static string AvatarPath => $"{EnvironmentDataProvider.EnvironmentName}/avatar";
    public static string GridPath => $"{EnvironmentDataProvider.EnvironmentName}/grid";
    public static string BacktracePath => $"{EnvironmentDataProvider.EnvironmentName}/backtrace";
    public static string MaintenancePath => $"{EnvironmentDataProvider.EnvironmentName}/maintenance";
    public static string CommandsPath => $"{EnvironmentDataProvider.EnvironmentName}/commands";
    public static string FloodCheckerPath => $"{EnvironmentDataProvider.EnvironmentName}/floodcheckers";
    public static string ScriptsPath => $"{EnvironmentDataProvider.EnvironmentName}/scripts";
    public static string ClientSettingsPath => $"{EnvironmentDataProvider.EnvironmentName}/client-settings";
    public static string GlobalPath => $"{EnvironmentDataProvider.EnvironmentName}/global";
    public static string WebPath => $"{EnvironmentDataProvider.EnvironmentName}/web";
    public static string GrpcPath => $"{EnvironmentDataProvider.EnvironmentName}/grpc";
}
