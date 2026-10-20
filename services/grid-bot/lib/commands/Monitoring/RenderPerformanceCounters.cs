namespace Grid.Bot;

using Prometheus;
using Grid.Bot.Metrics;

internal class RenderPerformanceCounters
{
    public static readonly Counter TotalRendersBlockedByGlobalFloodChecker = GridBotMetrics.Renders.BlockedByGlobalFloodChecker;
    public static readonly Counter TotalRendersBlockedByPerUserFloodChecker = GridBotMetrics.Renders.BlockedByPerUserFloodChecker;
    public static readonly Counter TotalRenders = GridBotMetrics.Renders.Total;
    public static readonly Counter TotalRendersViaUsername = GridBotMetrics.Renders.ViaUsername;
    public static readonly Counter TotalRendersWithInvalidIds = GridBotMetrics.Renders.WithInvalidIds;
    public static readonly Counter TotalRendersAgainstBannedUsers = GridBotMetrics.Renders.AgainstBannedUsers;
    public static readonly Counter TotalRendersWithErrors = GridBotMetrics.Renders.WithErrors;
    public static readonly Counter TotalRendersWithRbxThumbnailsErrors = GridBotMetrics.Renders.WithRbxThumbnailsErrors;
}