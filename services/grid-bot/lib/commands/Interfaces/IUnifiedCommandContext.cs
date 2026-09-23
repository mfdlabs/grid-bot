namespace Grid.Bot.Commands;

using Discord;

/// <summary>
/// Represents a unified command context for calling both slash and text commands.
/// </summary>
public interface IUnifiedCommandContext
{
  /// <summary>
  ///     Gets the <see cref="IDiscordClient" /> that the command is executed with.
  /// </summary>
  IDiscordClient Client { get; }

  /// <summary>
  ///     Gets the <see cref="IGuild" /> that the command is executed in.
  /// </summary>
  IGuild Guild { get; }

  /// <summary>
  ///     Gets the <see cref="IMessageChannel" /> that the command is executed in.
  /// </summary>
  IMessageChannel Channel { get; }

  /// <summary>
  ///     Gets the <see cref="IUser" /> who executed the command.
  /// </summary>
  IUser User { get; }

  /// <summary>
  ///     Gets the <see cref="IUserMessage" /> that the command is interpreted from.
  /// </summary>
  /// <remarks>This can be null in interaction contexts, use <see cref="IsMessage"/> for guarding.</remarks>
  IUserMessage Message { get; }

  /// <summary>
  ///     Gets the underlying interaction.
  /// </summary>
  /// <remarks>This can be null in interaction contexts, use <see cref="IsInteraction"/> for guarding.</remarks>
  IDiscordInteraction Interaction { get; }

  /// <summary>
  ///     Gets a value indicating whether the context is an interaction.
  /// </summary>
  bool IsInteraction { get; }

  /// <summary>
  ///     Gets a value indicating whether the context is a message.
  /// </summary>
  bool IsMessage { get; }
}