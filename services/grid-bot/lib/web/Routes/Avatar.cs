namespace Grid.Bot.Web.Routes;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Logging;
using Utility;
using Extensions;

using Threading.Extensions;

using Models;
using Models.V1;
using Models.V2;

using BodyColorsModelV1 = Grid.Bot.Web.Models.V1.BodyColorsModel;
using System.Net.Http.Json;


/// <summary>
/// Routes for the avatar API.
/// </summary>
public class Avatar
{
    private const string _avatarFetchCacheKeyFormat = "avatar_fetch:{0}:{1}";

    private const string _avatarFetchBodyColorsMapKey = "bodyColors";
    private const string _avatarFetchBodyColorsMapHeadColorKey = "headColorId";
    private const string _avatarFetchBodyColorsMapTorsoColorKey = "torsoColorId";
    private const string _avatarFetchBodyColorsMapRightArmColorKey = "rightArmColorId";
    private const string _avatarFetchBodyColorsMapLeftArmColorKey = "leftArmColorId";
    private const string _avatarFetchBodyColorsMapRightLegColorKey = "rightLegColorId";
    private const string _avatarFetchBodyColorsMapLeftLegColorKey = "leftLegColorId";

    private const int _gearAssetTypeId = 19;
    private static readonly int[] _animationAssetTypeIds = [
        48, // ClimbAnimation
        50, // FallAnimation
        51, // IdleAnimation
        52, // JumpAnimation
        53, // RunAnimation
        54, // SwimAnimation
        55, // WalkAnimation
        61, // EmoteAnimation
    ];

    private const string _robloxPlaceIdHeader = "Roblox-Place-Id";
    private const string _getAvatarFetchUserIdKey = "userId";
    private const string _getAvatarFetchPlaceIdKey = "placeId";
    private const string _getAvatarFetchUrlFormat = $"{{0}}/v1/users/{{1}}/avatar";

    private readonly ILogger _logger;
    private readonly AvatarSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ExpirableDictionary<string, AvatarFetchModel> _avatarFetchCache;

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

    private static AvatarFetchModel ConvertToAvatarFetchModel(AvatarModel avatarModel)
    {
        var model = new AvatarFetchModel
        {
            Scales = avatarModel.Scales,
            ResolvedAvatarType = avatarModel.PlayerAvatarType,
            BodyColors = BodyColorsModelV1.FromV2(avatarModel.BodyColors)
        };

        // Extract gear assets from avatarModel.Assets (backpack gear IDs not used)
        var equippedGearVersionIds = new List<long>();

        foreach (var asset in avatarModel.Assets)
            if (asset.AssetType.Id == _gearAssetTypeId)
                equippedGearVersionIds.Add(asset.CurrentVersionId);

        model.EquippedGearVersionIds = [.. equippedGearVersionIds];

        // Extract animation assets from avatarModel.Assets
        var animationAssetIds = new Dictionary<string, long>();
        foreach (var asset in avatarModel.Assets)
            if (Array.Exists(_animationAssetTypeIds, id => id == asset.AssetType.Id))
                animationAssetIds[asset.AssetType.Name] = asset.Id;

        model.AnimationAssetIds = animationAssetIds;

        // Extract all other assets from avatarModel.Assets
        var otherAssetIdAndTypeIds = new List<AssetIdAndTypeModel>();

        foreach (var asset in avatarModel.Assets)
            if (asset.AssetType.Id != _gearAssetTypeId && !Array.Exists(_animationAssetTypeIds, id => id == asset.AssetType.Id))
                otherAssetIdAndTypeIds.Add(new AssetIdAndTypeModel
                {
                    AssetId = asset.Id,
                    AssetTypeId = asset.AssetType.Id
                });

        model.AssetAndAssetTypeIds = [.. otherAssetIdAndTypeIds];

        return model;
    }

    private static string ConstructAvatarCacheKey(long userId, long placeId)
        => string.Format(_avatarFetchCacheKeyFormat, userId, placeId);

    private AvatarFetchModel GetAvatarFetchForUser(long userId, long placeId)
    {
        return _avatarFetchCache.GetOrAdd(
            ConstructAvatarCacheKey(userId, placeId),
            (key) => 
            {    
                _logger.Information("Cache miss for user {0} in place {1}", userId, placeId);

                using var httpClient = _httpClientFactory.CreateClient();
                var url = string.Format(_getAvatarFetchUrlFormat, _settings.AvatarApiUrl, userId);

                // Add the Roblox-Place-Id header to the request
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add(_robloxPlaceIdHeader, placeId.ToString());

                var response = httpClient.SendAsync(requestMessage).Sync();
                var avatarModel = response.Content.ReadFromJsonAsync<AvatarModel>().Sync();

                if (avatarModel == null)
                {
                    _logger.Warning("Failed to fetch avatar for user {0} in place {1}: No data returned", userId, placeId);

                    return null;
                }

                var avatarFetchModel = ConvertToAvatarFetchModel(avatarModel);
                
                return avatarFetchModel;
            });
    }

    /// <summary>
    /// Fetch the avatar for a user in a place.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" /></param>
    public async Task GetAvatarFetch(HttpContext context)
    {
        if (!context.Request.TryParseInt64FromQuery(_getAvatarFetchUserIdKey, out var userId) ||
            !context.Request.TryParseInt64FromQuery(_getAvatarFetchPlaceIdKey, out var placeId))
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
        await context.Response.WriteAsync(JsonConvert.SerializeObject(avatarFetchData));
    }
}
