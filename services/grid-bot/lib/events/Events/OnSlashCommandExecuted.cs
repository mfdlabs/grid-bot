namespace Grid.Bot.Events;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Discord.Interactions;

using Prometheus;

using Logging;

using Utility;
using Extensions;

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
        "bot_interactions_failed_total",
        "The total number of interactions failed.",
        "interaction_type",
        "command_name"
    );

    private static string GetGuildId(SocketInteraction interaction, IInteractionContext context)
    {
        return interaction.GetGuild(context.Client)?.Id.ToString() ?? "DM";
    }

    /// <summary>
    /// Invoke the handler.
    /// </summary>
    /// <param name="command">The <see cref="ICommandInfo"/>.</param>
    /// <param name="context">The <see cref="IInteractionContext"/>.</param>
    /// <param name="result">The <see cref="IResult"/>.</param>
    public async Task Invoke(ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (context.Interaction is not SocketInteraction interaction)
            return;

        if (!result.IsSuccess)
        {
            if (result.Error == InteractionCommandError.UnknownCommand) return;

            _totalInteractionsFailed.WithLabels(
                interaction.Type.ToString(),
                command.Name
            ).Inc();

            if (result is not ExecuteResult executeResult)
            {
                await interaction.FollowupAsync(result.ErrorReason);

                return;
            }

            var ex = executeResult.Exception;

            if (ex is InteractionException interactionException)
                ex = interactionException.InnerException;

            // Check if it is a Missing Permissions exception from Discord.
            if (ex is Discord.Net.HttpException httpException)
            {
                _logger.Warning("Got a discord HTTP exception ({0}), when executing command '{1}'", httpException.Message, command.ToString());

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
