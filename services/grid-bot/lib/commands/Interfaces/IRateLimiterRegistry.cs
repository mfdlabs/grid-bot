namespace Grid.Bot.Commands;

/// <summary>
/// The scope of a rate limit that rejected a call.
/// </summary>
public enum RateLimitScope
{
    /// <summary>
    /// The call was allowed.
    /// </summary>
    None,

    /// <summary>
    /// The global limit rejected the call.
    /// </summary>
    Global,

    /// <summary>
    /// The per-user limit rejected the call.
    /// </summary>
    PerUser,
}

/// <summary>
/// Registry for the script execution and render rate limiters.
/// </summary>
public interface IRateLimiterRegistry
{
    /// <summary>
    /// Try to take a script execution permit.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns><see cref="RateLimitScope.None"/> if allowed, otherwise the scope that rejected.</returns>
    RateLimitScope TryAcquireScriptExecution(ulong userId);

    /// <summary>
    /// Try to take a render permit.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns><see cref="RateLimitScope.None"/> if allowed, otherwise the scope that rejected.</returns>
    RateLimitScope TryAcquireRender(ulong userId);
}
