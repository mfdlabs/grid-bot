namespace MFDLabs.Grid.Commands;

/// <summary>
/// Settings for <see cref="ThumbnailCommand"/>
/// </summary>
public class ThumbnailSettings
{
    /// <summary>
    /// The type of thumbnail command.
    /// </summary>
    public ThumbnailCommandType Type { get; }

    /// <summary>
    /// The arguments to pass to the thumbnail command.
    /// </summary>
    public object[] Arguments { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ThumbnailSettings"/>
    /// </summary>
    /// <param name="type">The type of thumbnail command.</param>
    /// <param name="arguments">The arguments to pass to the thumbnail command.</param>
    public ThumbnailSettings(ThumbnailCommandType type, params object[] arguments)
    {
        Type = type;
        Arguments = arguments;
    }
}
