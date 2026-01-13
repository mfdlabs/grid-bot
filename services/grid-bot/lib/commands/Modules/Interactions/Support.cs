namespace Grid.Bot.Interactions.Public;

using System;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Discord;
using Discord.Interactions;

using Networking;

/// <summary>
/// Interaction handler for the support commands.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="Support"/>.
/// </remarks>
/// <param name="gridSettings">The <see cref="GridSettings"/>.</param>
/// <param name="globalSettings">The <see cref="GlobalSettings"/>.</param>
/// <param name="localIpAddressProvider">The <see cref="ILocalIpAddressProvider"/>.</param>
/// <param name="gridServerFileHelper">The <see cref="IGridServerFileHelper"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="gridSettings"/> cannot be null.
/// - <paramref name="globalSettings"/> cannot be null.
/// - <paramref name="localIpAddressProvider"/> cannot be null.
/// - <paramref name="gridServerFileHelper"/> cannot be null.
/// </exception>
[Group("support", "Commands used for grid-bot-support.")]
[IntegrationType(ApplicationIntegrationType.GuildInstall, ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
public class Support(
    GridSettings gridSettings,
    GlobalSettings globalSettings,
    ILocalIpAddressProvider localIpAddressProvider,
    IGridServerFileHelper gridServerFileHelper
    ) : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly GridSettings _gridSettings = gridSettings ?? throw new ArgumentNullException(nameof(gridSettings));
    private readonly GlobalSettings _globalSettings = globalSettings ?? throw new ArgumentNullException(nameof(globalSettings));
    private readonly ILocalIpAddressProvider _localIpAddressProvider = localIpAddressProvider ?? throw new ArgumentNullException(nameof(localIpAddressProvider));
    private readonly IGridServerFileHelper _gridServerFileHelper = gridServerFileHelper ?? throw new ArgumentNullException(nameof(gridServerFileHelper));

    /// <summary>
    /// Gets informational links for the bot, in a stylish embed.
    /// </summary>
    [SlashCommand("info", "Get information about Grid Bot.")]
    public async Task GetGeneralInformationAsync()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var gridServerVersion = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? FileVersionInfo.GetVersionInfo(_gridServerFileHelper.GetFullyQualifiedGridServerPath()).FileVersion
            : _gridSettings.GridServerImageTag;

        var embed = new EmbedBuilder()
            .WithTitle("Grid Bot")
            .WithDescription("Grid Bot is a Discord bot that provides a variety of features for interacting with Roblox Grid Servers, such as thumbnailing and Luau execution.")
            .WithColor(Color.Blue)
            .WithFooter("Grid Bot Support")
            .WithCurrentTimestamp()
            .AddField("Grid Bot Support Guild", _globalSettings.SupportGuildDiscordUrl)
            .AddField("Grid Bot Support Hub", _globalSettings.SupportHubGitHubUrl)
            .AddField("Grid Bot Documentation", _globalSettings.DocumentationHubUrl)
            .AddField("Machine Name", Environment.MachineName)
            .AddField("Machine Host", _localIpAddressProvider.GetHostName())
            .AddField("Local IP Address", _localIpAddressProvider.AddressV4)
            .AddField("Bot Version", informationalVersion)
            .AddField("Grid Server Version", gridServerVersion)
            .Build();

        await FollowupAsync(embed: embed);
    }
}
