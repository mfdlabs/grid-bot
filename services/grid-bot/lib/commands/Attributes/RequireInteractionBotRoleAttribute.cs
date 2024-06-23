namespace Grid.Bot.Interactions;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Interactions;

using Utility;

/// <summary>
/// An attribute that validates if the user has the required bot role.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="RequireBotRoleAttribute"/>.
/// </remarks>
/// <param name="botRole">The <see cref="BotRole"/>.</param>
public class RequireBotRoleAttribute(BotRole botRole = BotRole.Privileged) : PreconditionAttribute
{
    private readonly BotRole _botRole = botRole;


    /// <inheritdoc cref="PreconditionAttribute.CheckRequirementsAsync(IInteractionContext, ICommandInfo, IServiceProvider)"/>
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        var adminUtility = services.GetRequiredService<IAdminUtility>();

        switch (_botRole)
        {
            case BotRole.Privileged:
                if (!adminUtility.UserIsPrivilaged(context.User))
                    return Task.FromResult(PreconditionResult.FromError("You lack the permissions to use this command."));

                return Task.FromResult(PreconditionResult.FromSuccess());
            case BotRole.Administrator:
                if (!adminUtility.UserIsAdmin(context.User))
                    return Task.FromResult(PreconditionResult.FromError("You lack the permissions to use this command."));

                return Task.FromResult(PreconditionResult.FromSuccess());
            case BotRole.Owner:
                if (!adminUtility.UserIsOwner(context.User))
                    return Task.FromResult(PreconditionResult.FromError("You lack the permissions to use this command."));

                return Task.FromResult(PreconditionResult.FromSuccess());
            default:
                throw new ArgumentOutOfRangeException(nameof(_botRole), _botRole, null);
        }
    }
}
