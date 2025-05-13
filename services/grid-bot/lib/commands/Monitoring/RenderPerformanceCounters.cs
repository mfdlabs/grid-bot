namespace Grid.Bot;

using Prometheus;

internal class RenderPerformanceCounters
{
    public static readonly Counter TotalRendersBlockedByGlobalFloodChecker = Metrics.CreateCounter(
        "renders_blocked_by_global_flood_checker_total",
        "The total number of renders blocked by the global flood checker."
    );
    public static readonly Counter TotalRendersBlockedByPerUserFloodChecker = Metrics.CreateCounter(
        "renders_blocked_by_per_user_flood_checker_total",
        "The total number of renders blocked by the per user flood checker.",
        "user_id"
    );
    public static readonly Counter TotalRenders = Metrics.CreateCounter(
        "renders_total",
        "The total number of renders.",
        "user_name_or_id"
    );
    public static readonly Counter TotalRendersViaUsername = Metrics.CreateCounter(
        "renders_via_username_total",
        "The total number of renders via username."
    );
    public static readonly Counter TotalRendersWithInvalidIds = Metrics.CreateCounter(
        "renders_with_invalid_ids_total",
        "The total number of renders with invalid IDs."
    );
    public static readonly Counter TotalRendersAgainstBannedUsers = Metrics.CreateCounter(
        "renders_against_banned_users_total",
        "The total number of renders against banned users.",
        "user_name_or_id"
    );
    public static readonly Counter TotalRendersWithErrors = Metrics.CreateCounter(
        "renders_with_errors_total",
        "The total number of renders with errors."
    );
    public static readonly Counter TotalRendersWithRbxThumbnailsErrors = Metrics.CreateCounter(
        "renders_with_rbx_thumbnails_errors_total",
        "The total number of renders with rbx-thumbnails errors.",
        "state"
    );
}