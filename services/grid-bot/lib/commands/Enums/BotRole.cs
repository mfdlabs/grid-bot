namespace Grid.Bot;

/// <summary>
/// Bot role.
/// </summary>
public enum BotRole
{
    /// <summary>
    /// Default role. Used for everyone.
    /// </summary>
    Default,

    /// <summary>
    /// A privileged role. Mostly used for testers.
    /// </summary>
    Privileged,

    /// <summary>
    /// Administrator role. Used for administrators.
    /// </summary>
    Administrator,

    /// <summary>
    /// Owner role. Used for the owner of the bot.
    /// </summary>
    Owner
}
