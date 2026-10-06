namespace Grid.Bot;

using System;

/// <summary>
/// Options for all avatar related settings.
/// </summary>
public class AvatarOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Avatar";

    /// <summary>
    /// The vault path for the avatar options.
    /// </summary>
    public const string VaultPath = "avatar";

    /// <summary>
    /// Gets or sets the URL for the Avatar API.
    /// </summary>
    public string AvatarApiUrl { get; set; } = "https://avatar.roblox.com";

    /// <summary>
    /// Gets or sets the base url for the Users API site.
    /// </summary>
    public string UsersApiUrl { get; set; } = "https://users.roblox.com";

    /// <summary>
    /// Gets or sets the interval on which to traverse the avatar fetch cache to search for stale entries.
    /// </summary>
    public TimeSpan AvatarFetchCacheTraversalInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the TTL for each avatar-fetch cache entry.
    /// </summary>
    public TimeSpan AvatarFetchCacheEntryTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the url to be used for asset fetch.
    /// </summary>
    public string RenderAssetFetchUrl { get; set; } = "https://assetdelivery.roblox.com/v1";

    /// <summary>
    /// Gets or sets the thumbnail type for renders.
    /// </summary>
    public string RenderThumbnailType { get; set; } = "PNG";

    /// <summary>
    /// Gets or sets the timeout for render jobs.
    /// </summary>
    public TimeSpan RenderJobTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Gets or sets the url for avatar-fetch.
    /// </summary>
    public string AvatarFetchUrl { get; set; } = "https://avatar.sitetest4.robloxlabs.com/v1/avatar-fetch";

    /// <summary>
    /// Gets or sets the ID of the place used to inherit character settings from in avatar-fetch models.
    /// </summary>
    public long PlaceIdForRenders { get; set; } = 1818;

    /// <summary>
    /// Gets or sets the X dimension for renders.
    /// </summary>
    public int RenderXDimension { get; set; } = 720;

    /// <summary>
    /// Gets or sets the Y dimension for renders.
    /// </summary>
    public int RenderYDimension { get; set; } = 720;

    /// <summary>
    /// Gets or sets the rollout percentage for rbx-thumbnails.
    /// </summary>
    public int RbxThumbnailsRolloutPercent { get; set; } = 100;

    /// <summary>
    /// Gets or sets the url for rbx-thumbnails API.
    /// </summary>
    public string RbxThumbnailsUrl { get; set; } = "https://thumbnails.roblox.com";

    /// <summary>
    /// Gets or sets the TTL for the local cache.
    /// </summary>
    public TimeSpan LocalCacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets a list of user IDs that should be automatically blacklisted.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public long[] BlacklistUserIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the period to wait before persisting the blacklist.
    /// </summary>
    public TimeSpan BlacklistPersistPeriod { get; set; } = TimeSpan.FromMinutes(5);
}
