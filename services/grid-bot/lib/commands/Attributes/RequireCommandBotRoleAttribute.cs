namespace Grid.Bot.Commands;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Discord.Commands;

using Utility;

/// <summary>
/// An attribute that validates if the user has the required bot role.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="RequireBotRoleAttribute"/>.
/// </remarks>
/// <param name="botRole">The <see cref="Utility.BotRole"/>.</param>
public class RequireBotRoleAttribute(BotRole botRole = BotRole.Privileged) : PreconditionAttribute
{
    /// <summary>
    /// The role.
    /// </summary>
    public BotRole BotRole { get; } = botRole;
    
    private const string PermissionDeniedText = "You lack permission to execute this command.";

    /// <inheritdoc cref="PreconditionAttribute.CheckPermissionsAsync(ICommandContext, CommandInfo, IServiceProvider)"/>
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider services)
    {
        var adminUtility = services.GetRequiredService<IAdminUtility>();

        return BotRole switch
        {
            BotRole.Privileged => Task.FromResult(!adminUtility.UserIsPrivilaged(context.User)
                                ? PreconditionResult.FromError(PermissionDeniedText)
                                : PreconditionResult.FromSuccess()),
            BotRole.Administrator => Task.FromResult(!adminUtility.UserIsAdmin(context.User)
                                ? PreconditionResult.FromError(PermissionDeniedText)
                                : PreconditionResult.FromSuccess()),
            BotRole.Owner => Task.FromResult(!adminUtility.UserIsOwner(context.User)
                                ? PreconditionResult.FromError(PermissionDeniedText)
                                : PreconditionResult.FromSuccess()),
            _ => throw new ArgumentOutOfRangeException(nameof(BotRole), BotRole, null),
        };
    }
}
