namespace Grid.Bot.Web.Routes;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Logging;
using Utility;
using Extensions;

using Threading.Extensions;

/// <summary>
/// Routes for the avatar API.
/// </summary>
public class Avatar
{
    private const string AvatarFetchCacheKeyFormat = "avatar_fetch:{0}:{1}";

    private const string AvatarFetchBodyColorsMapKey = "bodyColors";
    private const string AvatarFetchBodyColorsMapHeadColorKey = "headColorId";
    private const string AvatarFetchBodyColorsMapTorsoColorKey = "torsoColorId";
    private const string AvatarFetchBodyColorsMapRightArmColorKey = "rightArmColorId";
    private const string AvatarFetchBodyColorsMapLeftArmColorKey = "leftArmColorId";
    private const string AvatarFetchBodyColorsMapRightLegColorKey = "rightLegColorId";
    private const string AvatarFetchBodyColorsMapLeftLegColorKey = "leftLegColorId";

    private const string GetAvatarFetchUserIdKey = "userId";
    private const string GetAvatarFetchPlaceIdKey = "placeId";
    private const string GetAvatarFetchUrlFormat = $"{{0}}/v1/avatar-fetch?{GetAvatarFetchUserIdKey}={{1}}&{GetAvatarFetchPlaceIdKey}={{2}}";

    private readonly ILogger _logger;
    private readonly AvatarSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ExpirableDictionary<string, dynamic> _avatarFetchCache;

    /// <summary>
    /// Construct a new instance of <see cref="Avatar" />
    /// </summary>
    /// <param name="logger">The <see cref="ILogger" /></param>
    /// <param name="settings">The <see cref="AvatarSettings" /></param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory" /></param>
    public Avatar(ILogger logger, AvatarSettings settings, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    
        _avatarFetchCache = new(_settings.AvatarFetchCacheEntryTtl, _settings.AvatarFetchCacheTraversalInterval);
    }

    private static string ConstructAvatarCacheKey(long userId, long placeId)
        => string.Format(AvatarFetchCacheKeyFormat, userId, placeId);

    private static dynamic DowngradeBodyColorsFormat(dynamic data)
    {
        var bodyColors = data[AvatarFetchBodyColorsMapKey];

        var newBodyColors = new 
        {
            HeadColor = bodyColors[AvatarFetchBodyColorsMapHeadColorKey],
            TorsoColor = bodyColors[AvatarFetchBodyColorsMapTorsoColorKey],
            RightArmColor = bodyColors[AvatarFetchBodyColorsMapRightArmColorKey],
            LeftArmColor = bodyColors[AvatarFetchBodyColorsMapLeftArmColorKey],
            RightLegColor = bodyColors[AvatarFetchBodyColorsMapRightLegColorKey],
            LeftLegColor = bodyColors[AvatarFetchBodyColorsMapLeftLegColorKey]
        };

        data[AvatarFetchBodyColorsMapKey] = JObject.FromObject(newBodyColors);

        return data;
    }


    private dynamic GetAvatarFetchForUser(long userId, long placeId)
    {
        return _avatarFetchCache.GetOrAdd(
            ConstructAvatarCacheKey(userId, placeId),
            _ => 
            {    
                _logger.Information("Cache miss for user {0} in place {1}", userId, placeId);

                using var httpClient = _httpClientFactory.CreateClient();
                var url = string.Format(GetAvatarFetchUrlFormat, _settings.AvatarApiUrl, userId, placeId);

                var response = httpClient.GetAsync(url).Sync();
                var data = response.Content.ReadAsStringAsync().Sync();

                var dynData = JsonConvert.DeserializeObject<dynamic>(data);

                if (_settings.AvatarFetchShouldDowngradeBodyColorsFormat)
                    dynData = DowngradeBodyColorsFormat(dynData);

                return dynData;
            });
    }

    /// <summary>
    /// Fetch the avatar for a user in a place.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" /></param>
    public async Task GetAvatarFetch(HttpContext context)
    {
        if (!context.Request.TryParseInt64FromQuery(GetAvatarFetchUserIdKey, out var userId) ||
            !context.Request.TryParseInt64FromQuery(GetAvatarFetchPlaceIdKey, out var placeId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteRbxError("Invalid user or place ID.");

            return;
        }

        var avatarFetchData = GetAvatarFetchForUser(userId, placeId);
        if (avatarFetchData == null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteRbxError("Avatar not found.");

            return;
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(text: (string)JsonConvert.SerializeObject(avatarFetchData));
    }
}
