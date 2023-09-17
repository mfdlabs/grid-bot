namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for all avatar related stuff.
/// </summary>
public class AvatarSettings : BaseSettingsProvider<AvatarSettings>
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.AvatarPath;

    /// <summary>
    /// Gets the url to be used for asset fetch.
    /// </summary>
    public string RenderAssetFetchUrl => GetOrDefault(
        nameof(RenderAssetFetchUrl),
        string.Empty
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
        "https://avatar.roblox.com/v1/avatar-fetch"
    );

    /// <summary>
    /// Gets the blacklisted usernames for rendering, helps deter issues with specific users.
    /// </summary>
    public string[] BlacklistedUsernamesForRendering => GetOrDefault(
        nameof(BlacklistedUsernamesForRendering),
        Array.Empty<string>()
    );

    /// <summary>
    /// Gets the ID of the place used to inherit character settings from in avatar-fetch models.
    /// </summary>
    public long PlaceIdForRenders => GetOrDefault(
        nameof(PlaceIdForRenders),
        1818
    );

    /// <summary>
    /// Gets the X dimension for renders.
    /// </summary>
    public int RenderXDimension => GetOrDefault(
        nameof(RenderXDimension),
        1068
    );

    /// <summary>
    /// Gets the Y dimension for renders.
    /// </summary>
    public int RenderYDimension => GetOrDefault(
        nameof(RenderYDimension),
        1068
    );
}
