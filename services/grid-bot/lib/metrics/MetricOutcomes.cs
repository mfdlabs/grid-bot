namespace Grid.Bot.Metrics;

/// <summary>
/// The bounded set of values used for the <c>outcome</c> label.
/// </summary>
public static class MetricOutcomes
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    public const string Success = "success";

    /// <summary>
    /// The operation failed because of invalid input or a user-facing error.
    /// </summary>
    public const string UserError = "user_error";

    /// <summary>
    /// The operation exceeded its time limit.
    /// </summary>
    public const string Timeout = "timeout";

    /// <summary>
    /// The operation failed with an unexpected error.
    /// </summary>
    public const string Exception = "exception";

    /// <summary>
    /// The operation was refused before it ran, such as by maintenance mode, a blacklist, or a flood checker.
    /// </summary>
    public const string Rejected = "rejected";

    /// <summary>
    /// The requested command could not be found.
    /// </summary>
    public const string NotFound = "not_found";

    /// <summary>
    /// Every outcome.
    /// </summary>
    public static readonly string[] All = [Success, UserError, Timeout, Exception, Rejected, NotFound];
}
