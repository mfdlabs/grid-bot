namespace Grid.Bot.Events;

using System;

using Discord.Commands;
using Discord.Interactions;

using Grid.Bot.Metrics;

using CommandResult = Discord.Commands.IResult;
using InteractionResult = Discord.Interactions.IResult;

/// <summary>
/// Maps Discord.Net command and interaction results to the bounded <c>outcome</c> metric label.
/// </summary>
internal static class OutcomeClassifier
{
    /// <summary>
    /// Classifies the result of a text command.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/>.</param>
    /// <returns>A value from <see cref="MetricOutcomes"/>.</returns>
    public static string FromCommandResult(CommandResult result)
    {
        if (result.IsSuccess)
            return MetricOutcomes.Success;

        if (result.Error == CommandError.UnknownCommand)
            return MetricOutcomes.NotFound;

        if (result is Discord.Commands.ExecuteResult executeResult)
            return FromException(executeResult.Exception is CommandException commandException
                ? commandException.InnerException
                : executeResult.Exception);

        return result.Error == CommandError.Exception
            ? MetricOutcomes.Exception
            : MetricOutcomes.UserError;
    }

    /// <summary>
    /// Classifies the result of an interaction.
    /// </summary>
    /// <param name="result">The <see cref="InteractionResult"/>.</param>
    /// <returns>A value from <see cref="MetricOutcomes"/>.</returns>
    public static string FromInteractionResult(InteractionResult result)
    {
        if (result.IsSuccess)
            return MetricOutcomes.Success;

        if (result.Error == InteractionCommandError.UnknownCommand)
            return MetricOutcomes.NotFound;

        if (result is Discord.Interactions.ExecuteResult executeResult)
            return FromException(executeResult.Exception is InteractionException interactionException
                ? interactionException.InnerException
                : executeResult.Exception);

        return result.Error == InteractionCommandError.Exception
            ? MetricOutcomes.Exception
            : MetricOutcomes.UserError;
    }

    // The bot throws ApplicationException for errors that are meant to be shown to the user.
    private static string FromException(Exception exception) => exception switch
    {
        ApplicationException => MetricOutcomes.UserError,
        TimeoutException or OperationCanceledException => MetricOutcomes.Timeout,
        _ => MetricOutcomes.Exception,
    };
}
