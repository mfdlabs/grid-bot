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
/// <param name="botRole">The <see cref="BotRole"/>.</param>
public class RequireBotRoleAttribute(BotRole botRole = BotRole.Privileged) : PreconditionAttribute
{
    private readonly BotRole _botRole = botRole;
    
    private const string _permissionDeniedText = "You lack permission to execute this command.";


    /// <inheritdoc cref="PreconditionAttribute.CheckPermissionsAsync(ICommandContext, CommandInfo, IServiceProvider)"/>
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider services)
    {
        var adminUtility = services.GetRequiredService<IAdminUtility>();

        return _botRole switch
        {
            BotRole.Privileged => Task.FromResult(!adminUtility.UserIsPrivilaged(context.User)
                                ? PreconditionResult.FromError(_permissionDeniedText)
                                : PreconditionResult.FromSuccess()),
            BotRole.Administrator => Task.FromResult(!adminUtility.UserIsPrivilaged(context.User)
                                ? PreconditionResult.FromError(_permissionDeniedText)
                                : PreconditionResult.FromSuccess()),
            BotRole.Owner => Task.FromResult(!adminUtility.UserIsOwner(context.User)
                                ? PreconditionResult.FromError(_permissionDeniedText)
                                : PreconditionResult.FromSuccess()),
            _ => throw new ArgumentOutOfRangeException(nameof(_botRole), _botRole, null),
        };

    }
}
