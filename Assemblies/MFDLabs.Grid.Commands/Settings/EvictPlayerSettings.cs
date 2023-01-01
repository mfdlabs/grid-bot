namespace MFDLabs.Grid.Commands;

/// <summary>
/// Settings for <see cref="EvictPlayerCommand"/>
/// </summary>
public class EvictPlayerSettings
{
    /// <summary>
    /// The ID of the player to evict.
    /// </summary>
    public long PlayerId { get; }

    /// <summary>
    /// Construct a new instance of <see cref="EvictPlayerSettings"/>
    /// </summary>
    /// <param name="playerId">The ID of the player to evict.</param>
    public EvictPlayerSettings(long playerId) => PlayerId = playerId;
}
