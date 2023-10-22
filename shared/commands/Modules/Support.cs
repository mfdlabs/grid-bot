namespace Grid.Bot.Interactions;

using System;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;

using Networking;

/// <summary>
/// Interaction handler for the support commands.
/// </summary>
[Group("support", "Commands used for grid-bot-support.")]
public class Support : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly GridSettings _gridSettings;
    private readonly GlobalSettings _globalSettings;
    private readonly ILocalIpAddressProvider _localIpAddressProvider;

    /// <summary>
    /// Construct a new instance of <see cref="Support"/>.
    /// </summary>
    /// <param name="gridSettings">The <see cref="GridSettings"/>.</param>
    /// <param name="globalSettings">The <see cref="GlobalSettings"/>.</param>
    /// <param name="localIpAddressProvider">The <see cref="ILocalIpAddressProvider"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="gridSettings"/> cannot be null.
    /// - <paramref name="globalSettings"/> cannot be null.
    /// - <paramref name="localIpAddressProvider"/> cannot be null.
    /// </exception>
    public Support(
        GridSettings gridSettings,
        GlobalSettings globalSettings,
        ILocalIpAddressProvider localIpAddressProvider
    )
    {
        _gridSettings = gridSettings ?? throw new ArgumentNullException(nameof(gridSettings));
        _globalSettings = globalSettings ?? throw new ArgumentNullException(nameof(globalSettings));
        _localIpAddressProvider = localIpAddressProvider ?? throw new ArgumentNullException(nameof(localIpAddressProvider));
    }

    /// <summary>
    /// Gets informational links for the bot, in a stylish embed.
    /// </summary>
    [SlashCommand("info", "Get information about Grid Bot.")]
    public async Task GetGeneralInformationAsync()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var informationalVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

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
            .AddField("Grid Server Version", _gridSettings.GridServerImageTag)
            .Build();

        await FollowupAsync(embed: embed);
    }
}
