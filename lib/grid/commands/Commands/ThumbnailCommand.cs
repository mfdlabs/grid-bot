namespace Grid.Commands;

/// <summary>
/// Command for rendering thumbnails.
/// </summary>
public class ThumbnailCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode { get; }

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The settings for the command.
    /// </summary>
    public ThumbnailSettings Settings { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ThumbnailCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command.</param>
    public ThumbnailCommand(ThumbnailSettings settings)
    {
        Settings = settings;

        Mode = Settings.Type == ThumbnailCommandType.TexturePack 
            ? "ExecuteScript" 
            : "Thumbnail";
    }
}
