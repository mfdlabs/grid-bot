namespace Grid.Bot.UnifiedCommands.Public;

using System;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

using Discord;
using Discord.Commands;
using Discord.Interactions;

using Microsoft.Extensions.Options;

using Grid.ProcessManagement;

using Grid.Bot.Commands;

using TextCommandGroup = Discord.Commands.GroupAttribute;
using TextCommandSummary = Discord.Commands.SummaryAttribute;
using InteractionGroup = Discord.Interactions.GroupAttribute;

using TextCommandModuleBase = Discord.Commands.ModuleBase;
using InteractionModuleBase = Discord.Interactions.InteractionModuleBase;


/// <remarks>
/// Construct a new instance of <see cref="Support"/>.
/// </remarks>
/// <param name="gridOptions">The <see cref="GridOptions"/>.</param>
/// <param name="globalOptions">The <see cref="GlobalOptions"/>.</param>
/// <param name="gridServerFileHelper">The <see cref="IGridServerFileHelper"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="gridOptions"/> cannot be null.
/// - <paramref name="globalOptions"/> cannot be null.
/// - <paramref name="gridServerFileHelper"/> cannot be null.
/// </exception>
public class Support(
    IOptionsMonitor<GridOptions> gridOptions,
    IOptionsMonitor<GlobalOptions> globalOptions,
    IGridServerFileHelper gridServerFileHelper
)
{
    private readonly IOptionsMonitor<GridOptions> _gridOptions = gridOptions ?? throw new ArgumentNullException(nameof(gridOptions));

    private GridOptions _gridSettings => _gridOptions.CurrentValue;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions = globalOptions ?? throw new ArgumentNullException(nameof(globalOptions));
    private readonly IGridServerFileHelper _gridServerFileHelper = gridServerFileHelper ?? throw new ArgumentNullException(nameof(gridServerFileHelper));

    private static bool GetAddressByInterface(NetworkInterfaceType interfaceType, out string ip)
        => !string.IsNullOrEmpty(
            ip = NetworkInterface.GetAllNetworkInterfaces()
            .Where(item => item.NetworkInterfaceType == interfaceType &&
                            item.OperationalStatus == OperationalStatus.Up)
            .Select(item => item.GetIPProperties().UnicastAddresses)
            .Select(item => item.FirstOrDefault()?.Address)
            .FirstOrDefault()?
            .ToString()
        );

    private static string GetLocalAddress()
    {
        if (GetAddressByInterface(NetworkInterfaceType.Wireless80211, out var ip))
            return ip;

        if (GetAddressByInterface(NetworkInterfaceType.Ethernet, out ip))
            return ip;

        GetAddressByInterface(NetworkInterfaceType.Loopback, out ip);

        return ip;
    }

    /// <summary>
    /// Gets informational links for the bot, in a stylish embed.
    /// </summary>
    public async Task GetGeneralInformationAsync(IUnifiedCommandContext context)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var informationalVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        var gridServerVersion = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? FileVersionInfo.GetVersionInfo(_gridServerFileHelper.GetFullyQualifiedGridServerPath()).FileVersion
            : _gridSettings.GridServerImageTag;

        var embed = new EmbedBuilder()
            .WithTitle("Grid Bot")
            .WithDescription("Grid Bot is a Discord bot that provides a variety of features for interacting with Roblox Grid Servers, such as thumbnailing and Luau execution.")
            .WithColor(Color.Blue)
            .WithFooter("Grid Bot Support")
            .WithCurrentTimestamp()
            .AddField("Grid Bot Support Guild", _globalOptions.CurrentValue.SupportGuildDiscordUrl)
            .AddField("Grid Bot Support Hub", _globalOptions.CurrentValue.SupportHubGitHubUrl)
            .AddField("Grid Bot Documentation", _globalOptions.CurrentValue.DocumentationHubUrl)
            .AddField("Machine Name", Environment.MachineName)
            .AddField("Machine Host", Dns.GetHostName())
            .AddField("Local IP Address", GetLocalAddress())
            .AddField("Bot Version", informationalVersion)
            .AddField("Grid Server Version", gridServerVersion)
            .Build();

        await context.RespondAsync(embed: embed).ConfigureAwait(false);
    }
}

#region MODULE PROXIES

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[TextCommandGroup("support"), TextCommandSummary("Commands used for grid-bot-support.")]
public class SupportTextCommand(Support supportCommand) : TextCommandModuleBase
{
    private readonly Support _supportCommand = supportCommand ?? throw new ArgumentNullException(nameof(supportCommand));

    [Command("info"), TextCommandSummary("Get information about Grid Bot."), Alias("information", "about")]
    public async Task GetGeneralInformationAsync()
        => await _supportCommand.GetGeneralInformationAsync(new UnifiedCommandContext(Context)).ConfigureAwait(false);
}


[InteractionGroup("support", "Commands used for grid-bot-support.")]
[IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public class SupportInteraction(Support supportCommand) : InteractionModuleBase
{
    private readonly Support _supportCommand = supportCommand ?? throw new ArgumentNullException(nameof(supportCommand));

    [SlashCommand("info", "Get information about Grid Bot.")]
    public async Task GetGeneralInformationAsync()
        => await _supportCommand.GetGeneralInformationAsync(new UnifiedCommandContext(Context)).ConfigureAwait(false);
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endregion