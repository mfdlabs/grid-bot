namespace Grid.Commands;

/// <summary>
/// The settings for <see cref="ServerActionCommand"/>
/// </summary>
public class ServerActionSettings
{
    /// <summary>
    /// The action.
    /// </summary>
    public ServerActionType Action { get; }

    /// <summary>
    /// The reason.
    /// </summary>
    public ServerActionReason Reason { get; }

    /// <summary>
    /// The verbose reason.
    /// </summary>
    public string VerboseReason { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ServerActionSettings"/>
    /// </summary>
    /// <param name="serverActionType">The action.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="verboseReason">The verbose reason.</param>
    public ServerActionSettings(ServerActionType serverActionType, ServerActionReason reason, string verboseReason)
    {
        Action = serverActionType;
        Reason = reason;
        VerboseReason = verboseReason;
    }
}
