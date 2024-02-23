namespace Grid.Bot.Events;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;

using Prometheus;

using Logging;

using Utility;

/// <summary>
/// Invoked when slash commands are executed.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnInteractionExecuted"/>.
/// </remarks>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="backtraceUtility">The <see cref="BacktraceUtility"/>.</param>
/// <param name="discordRolesSettings">The <see cref="DiscordRolesSettings"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="backtraceUtility"/> cannot be null.
/// - <paramref name="discordRolesSettings"/> cannot be null.
/// </exception>
public class OnInteractionExecuted(
    ILogger logger,
    IBacktraceUtility backtraceUtility,
    DiscordRolesSettings discordRolesSettings
)
{
    private const string UnhandledExceptionOccurredFromCommand = "An error occured with the command:";

    private readonly DiscordRolesSettings _discordRolesSettings = discordRolesSettings ?? throw new ArgumentNullException(nameof(discordRolesSettings));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IBacktraceUtility _backtraceUtility = backtraceUtility ?? throw new ArgumentNullException(nameof(backtraceUtility));

    private readonly Counter _totalInteractionsFailed = Metrics.CreateCounter(
        "grid_interactions_failed_total",
        "The total number of interactions failed.",
        "interaction_type",
        "interaction_id",
        "interaction_user_id",
        "interaction_channel_id",
        "interaction_guild_id"
    );

    private string GetGuildId(IInteractionContext context)
    {
        return context.Guild?.Id.ToString() ?? "DM";
    }

    /// <summary>
    /// Invoke the handler.
    /// </summary>
    /// <param name="command">The <see cref="ICommandInfo"/>.</param>
    /// <param name="context">The <see cref="IInteractionContext"/>.</param>
    /// <param name="result">The <see cref="IResult"/>.</param>
    public async Task Invoke(ICommandInfo command, IInteractionContext context, IResult result)
    {
        var interaction = context.Interaction;

        if (!result.IsSuccess)
        {
            if (result.Error == InteractionCommandError.UnknownCommand) return;

            _totalInteractionsFailed.WithLabels(
                interaction.Type.ToString(),
                interaction.Id.ToString(),
                interaction.User.Id.ToString(),
                interaction.ChannelId.ToString(),
                GetGuildId(context)
            ).Inc();

            if (result is not ExecuteResult executeResult)
            {
                await interaction.FollowupAsync(result.ErrorReason);

                return;
            }

            var ex = executeResult.Exception;

            // Check if it is a Missing Permissions exception from Discord.
            if (ex is Discord.Net.HttpException httpException && httpException.DiscordCode == DiscordErrorCode.MissingPermissions)
            {
                _logger.Warning("Missing permissions for command {0} in guild {1}", command.Name, context.Guild.Id);

                return;
            }

            if (ex is not ApplicationException)
                _backtraceUtility.UploadException(ex);

            var exceptionId = Guid.NewGuid();

            switch (ex)
            {
                case ApplicationException _:
                    _logger.Warning("Application threw an exception {0}", ex.ToString());

                    await interaction.FollowupAsync(
                        text: ex.Message
                    );

                    return;
            }

            _logger.Error("[EID-{0}] An unexpected error occurred: {1}", exceptionId.ToString(), ex.ToString());

#if DEBUG
            var detail = ex.ToString();
            if (detail.Length > EmbedBuilder.MaxDescriptionLength)
            {
                await interaction.FollowupWithFileAsync(
                    fileStream: new MemoryStream(Encoding.UTF8.GetBytes(detail)),
                    fileName: $"{exceptionId}.txt",
                    text: UnhandledExceptionOccurredFromCommand
                );

                return;
            }

            await interaction.FollowupAsync(
                UnhandledExceptionOccurredFromCommand,
                embed: new EmbedBuilder().WithDescription($"```\n{ex}\n```").Build()
            );

            return;
#else

            await interaction.FollowupAsync(
                $"An unexpected Exception has occurred. Exception ID: {exceptionId}, send this ID to " +
                $"<@!{_discordRolesSettings.BotOwnerId}>"
            );
#endif
        }
    }
}
