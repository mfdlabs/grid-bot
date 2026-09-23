namespace Grid.Bot.Commands;

using System;

using Discord;
using Discord.Commands;

/// <summary>
/// Default implementation for <see cref="IUnifiedCommandContext"/>
/// </summary>
internal class UnifiedCommandContext : IUnifiedCommandContext
{
    /// <summary>
    /// Construct a new <see cref="UnifiedCommandContext"/> from a <see cref="ICommandContext"/>
    /// </summary>
    /// <param name="commandContext">The <see cref="ICommandContext"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="commandContext"/> cannot be null.</exception>
    public UnifiedCommandContext(ICommandContext commandContext)
    {
        ArgumentNullException.ThrowIfNull(commandContext, nameof(commandContext));
        
        Client = commandContext.Client;
        Guild = commandContext.Guild;
        Channel = commandContext.Channel;
        User = commandContext.User;
        Message = commandContext.Message;
    }

    /// <summary>
    /// Construct a new <see cref="UnifiedCommandContext"/> from a <see cref="IInteractionContext"/>
    /// </summary>
    /// <param name="interactionContext">The <see cref="IInteractionContext"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="interactionContext"/> cannot be null.</exception>
    public UnifiedCommandContext(IInteractionContext interactionContext)
    {
        ArgumentNullException.ThrowIfNull(interactionContext, nameof(interactionContext));
        
        Client = interactionContext.Client;
        Guild = interactionContext.Guild;
        Channel = interactionContext.Channel;
        User = interactionContext.User;
        Interaction = interactionContext.Interaction;
    }

    /// <inheritdoc cref="IUnifiedCommandContext.Client"/>
    public IDiscordClient Client { get; }

    /// <inheritdoc cref="IUnifiedCommandContext.Guild"/>
    public IGuild Guild { get; }

    /// <inheritdoc cref="IUnifiedCommandContext.Channel"/>
    public IMessageChannel Channel { get; }

    /// <inheritdoc cref="IUnifiedCommandContext.User"/>
    public IUser User { get; }

    /// <inheritdoc cref="IUnifiedCommandContext.Message"/>
    public IUserMessage Message { get; } = null;

    /// <inheritdoc cref="IUnifiedCommandContext.Interaction"/>
    public IDiscordInteraction Interaction { get; } = null;

    /// <inheritdoc cref="IUnifiedCommandContext.IsInteraction"/>
    public bool IsInteraction => Interaction is not null;

    /// <inheritdoc cref="IUnifiedCommandContext.IsMessage"/>
    public bool IsMessage => Message is not null;

}