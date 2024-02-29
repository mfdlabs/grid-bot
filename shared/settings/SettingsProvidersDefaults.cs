namespace Grid.Bot;

using System;

internal static class SettingsProvidersDefaults
{
    private const string _vaultMountEnvVar = "VAULT_MOUNT";

#if DEBUG
    private const string ProviderPathConfiguration = "debug";
#else
    private const string ProviderPathConfiguration = "release";
#endif

    public const string ProviderPathPrefix = "grid-bot";

    public const string DiscordPath = $"{ProviderPathPrefix}/discord/{ProviderPathConfiguration}";
    public const string DiscordRolesPath = $"{ProviderPathPrefix}/discord-roles/{ProviderPathConfiguration}";
    public const string AvatarPath = $"{ProviderPathPrefix}/avatar/{ProviderPathConfiguration}";
    public const string GridPath = $"{ProviderPathPrefix}/grid/{ProviderPathConfiguration}";
    public const string BacktracePath = $"{ProviderPathPrefix}/backtrace/{ProviderPathConfiguration}";
    public const string MaintenancePath = $"{ProviderPathPrefix}/maintenance/{ProviderPathConfiguration}";
    public const string CommandsPath = $"{ProviderPathPrefix}/commands/{ProviderPathConfiguration}";
    public const string FloodCheckerPath = $"{ProviderPathPrefix}/floodcheckers/{ProviderPathConfiguration}";
    public const string ConsulPath = $"{ProviderPathPrefix}/consul/{ProviderPathConfiguration}";
    public const string UsersClientPath = $"{ProviderPathPrefix}/users-client/{ProviderPathConfiguration}";
    public const string ScriptsPath = $"{ProviderPathPrefix}/scripts/{ProviderPathConfiguration}";
    public const string ClientSettingsClientPath = $"{ProviderPathPrefix}/client-settings-client/{ProviderPathConfiguration}";
    public const string GlobalPath = $"{ProviderPathPrefix}/global/{ProviderPathConfiguration}";

    public const string DefaultMountPath = "grid-bot-settings";
    public static string MountPath = Environment.GetEnvironmentVariable(_vaultMountEnvVar) ?? DefaultMountPath;
}
