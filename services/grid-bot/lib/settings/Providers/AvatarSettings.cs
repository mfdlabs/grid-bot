namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for all avatar related stuff.
/// </summary>
public class AvatarSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.AvatarPath;

    
    /// <summary>
    /// Gets the URL for the Avatar API.
    /// </summary>
    public string AvatarApiUrl => GetOrDefault(nameof(AvatarApiUrl), "https://avatar.roblox.com");

    /// <summary>
    /// Gets the interval on which to traverse the avatar fetch cache to search for stale entries.
    /// </summary>
    public TimeSpan AvatarFetchCacheTraversalInterval => GetOrDefault(nameof(AvatarFetchCacheTraversalInterval), TimeSpan.FromMinutes(5));

    /// <summary>
    /// Gets the TTL for each avatar-fetch cache entry.
    /// </summary>
    public TimeSpan AvatarFetchCacheEntryTtl => GetOrDefault(nameof(AvatarFetchCacheEntryTtl), TimeSpan.FromMinutes(5));

    /// <summary>
    /// Determines whether or not body colors are downgraded to pre v2 (v1.1).
    /// </summary>
    public bool AvatarFetchShouldDowngradeBodyColorsFormat => GetOrDefault(nameof(AvatarFetchShouldDowngradeBodyColorsFormat), true);

    /// <summary>
    /// Gets the url to be used for asset fetch.
    /// </summary>
    public string RenderAssetFetchUrl => GetOrDefault(
        nameof(RenderAssetFetchUrl),
        "https://assetdelivery.roblox.com/v1"
    );

    /// <summary>
    /// Gets the thumbnail type for renders.
    /// </summary>
    public string RenderThumbnailType => GetOrDefault(
        nameof(RenderThumbnailType),
        "PNG"
    );

    /// <summary>
    /// Gets the timeout for render jobs.
    /// </summary>
    public TimeSpan RenderJobTimeout => GetOrDefault(
        nameof(RenderJobTimeout),
        TimeSpan.FromSeconds(20)
    );

    /// <summary>
    /// Gets the url for avatar-fetch.
    /// </summary>
    public string AvatarFetchUrl => GetOrDefault(
        nameof(AvatarFetchUrl),
        "https://avatar.sitetest4.robloxlabs.com/v1/avatar-fetch"
    );

    /// <summary>
    /// Gets the ID of the place used to inherit character settings from in avatar-fetch models.
    /// </summary>
    public long PlaceIdForRenders => GetOrDefault<long>(
        nameof(PlaceIdForRenders),
        1818
    );

    /// <summary>
    /// Gets the X dimension for renders.
    /// </summary>
    public int RenderXDimension => GetOrDefault(
        nameof(RenderXDimension),
        720
    );

    /// <summary>
    /// Gets the Y dimension for renders.
    /// </summary>
    public int RenderYDimension => GetOrDefault(
        nameof(RenderYDimension),
        720
    );

    /// <summary>
    /// Gets the rollout percentage for rbx-thumbnails.
    /// </summary>
    public int RbxThumbnailsRolloutPercent => GetOrDefault(
        nameof(RbxThumbnailsRolloutPercent),
        0
    );

    /// <summary>
    /// Gets the url for rbx-thumbnails API.
    /// </summary>
    public string RbxThumbnailsUrl => GetOrDefault(
        nameof(RbxThumbnailsUrl),
        "https://thumbnails.roblox.com"
    );

    /// <summary>
    /// Gets the render dimensions.
    /// </summary>
    public Thumbnails.Client.Size RenderDimensions => GetOrDefault(
        nameof(RenderDimensions),
        Thumbnails.Client.Size._720x720
    );

    /// <summary>
    /// Gets the TTL for the local cache.
    /// </summary>
    public TimeSpan LocalCacheTtl => GetOrDefault(
        nameof(LocalCacheTtl),
        TimeSpan.FromMinutes(5)
    );

    /// <summary>
    /// A list of user IDs that should be automatically blacklisted.
    /// </summary>
    public long[] BlacklistUserIds {
        get => GetOrDefault(
            nameof(BlacklistUserIds),
            Array.Empty<long>()
        );
        set => Set(nameof(BlacklistUserIds), value);
    }

    /// <summary>
    /// Gets the period to wait before persisting the blacklist.
    /// </summary>
    public TimeSpan BlacklistPersistPeriod => GetOrDefault(
        nameof(BlacklistPersistPeriod),
        TimeSpan.FromMinutes(5)
    );
}
