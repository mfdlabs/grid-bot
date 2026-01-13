namespace Grid.Bot.Commands;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

using Utility;

/// <summary>
/// An attribute only allows the command to be executed within the DMs of
/// users within the specified bot role, or within the specified guild (if configured).
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="LockDownCommandAttribute"/>.
/// </remarks>
/// <param name="botRole">The <see cref="BotRole"/>.</param>
public class LockDownCommandAttribute(BotRole botRole = BotRole.Administrator) : PreconditionAttribute
{
    /// <summary>
    /// The marker to indicate that the command should not respond.
    /// </summary>
    public const string MarkerDoNotRespond = "___||DO_NOT_RESPOND||___";

    /// <inheritdoc cref="PreconditionAttribute.CheckPermissionsAsync(ICommandContext, CommandInfo, IServiceProvider)"/>
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider services)
    {
        var commandsSettings = services.GetRequiredService<CommandsSettings>();
        if (!commandsSettings.EnableLockdownCommands)
            return Task.FromResult(PreconditionResult.FromSuccess());

        if (context.Guild is not null)
            return context.Guild.Id == commandsSettings.LockdownGuildId
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError(MarkerDoNotRespond));

        var adminUtility = services.GetRequiredService<IAdminUtility>();

        var isInRole = botRole switch
        {
            BotRole.Privileged => adminUtility.UserIsPrivilaged(context.User),
            BotRole.Administrator => adminUtility.UserIsAdmin(context.User),
            BotRole.Owner => adminUtility.UserIsOwner(context.User),
            BotRole.Default or _ => throw new ArgumentOutOfRangeException(nameof(botRole), botRole, null),
        };

        return isInRole
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError(MarkerDoNotRespond));
    }
}
