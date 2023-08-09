namespace MFDLabs.Grid.Commands;

using System;

/// <summary>
/// Settings for <see cref="GameServerCommand"/>
/// </summary>
public class GameServerSettings
{
    /// <summary>
    /// The ID of the place.
    /// </summary>
    public long PlaceId { get; }

    /// <summary>
    /// The ID of the universe.
    /// </summary>
    public long UniverseId { get; }

    /// <summary>
    /// The matchmaking context ID.
    /// </summary>
    public int MatchmakingContextId { get; }

    /// <summary>
    /// The signature of the job.
    /// </summary>
    public string JobSignature { get; }

    /// <summary>
    /// The game code.
    /// </summary>
    public string GameCode { get; }

    /// <summary>
    /// The base url.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// The ID of the game.
    /// </summary>
    public Guid GameId { get; }

    /// <summary>
    /// The machine address of the server.
    /// </summary>
    public string MachineAddress { get; }

    /// <summary>
    /// The ID of the server.
    /// </summary>
    public int ServerId { get; }

    /// <summary>
    /// The interval to collect google analytics metrics.
    /// </summary>
    public int GsmInterval { get; }

    /// <summary>
    /// The maximum amount of players allowed.
    /// </summary>
    public int MaxPlayers { get; }

    /// <summary>
    /// The maximum amount of game instances allowed.
    /// </summary>
    public int MaxGameInstances { get; }

    /// <summary>
    /// The games-service/game-instances-service api key.
    /// </summary>
    public string ApiKey { get; }

    /// <summary>
    /// The preferred amount of players.
    /// </summary>
    public int PreferredPlayerCapacity { get; }

    /// <summary>
    /// The place visit access key.
    /// </summary>
    public string PlaceVisitAccessKey { get; }

    /// <summary>
    /// The ID of the host datacenter.
    /// </summary>
    public int DatacenterId { get; }

    /// <summary>
    /// The ID of the creator.
    /// </summary>
    public long CreatorId { get; }

    /// <summary>
    /// The type of the creator.
    /// </summary>
    public string CreatorType { get; }

    /// <summary>
    /// The version of the place.
    /// </summary>
    public int PlaceVersion { get; }

    /// <summary>
    /// The ID of the VIP server owner.
    /// </summary>
    public long? VipOwnerId { get; }

    /// <summary>
    /// The url to fetch the place from.
    /// </summary>
    public string PlaceFetchUrl { get; }

    /// <summary>
    /// The optional metadata.
    /// </summary>
    public string Metadata { get; }

    /// <summary>
    /// The preferred port to use.
    /// </summary>
    public int? PreferredPort { get; }

    /// <summary>
    /// Construct a new instance of <see cref="GameServerSettings"/>
    /// </summary>
    /// <param name="placeId">The ID of the place.</param>
    /// <param name="universeId">The ID of the universe.</param>
    /// <param name="matchmakingContextId">The context to match make with.</param>
    /// <param name="jobSignature">The signature for the job.</param>
    /// <param name="gameCode">The code for the game.</param>
    /// <param name="baseUrl">The base url.</param>
    /// <param name="gameId">The ID of the game.</param>
    /// <param name="machineAddress">The address of the machine.</param>
    /// <param name="serverId">The ID of the host server.</param>
    /// <param name="gsmInterval">The inteval to collect google analytics metrics.</param>
    /// <param name="maxPlayers">The maximum amount of players in a game.</param>
    /// <param name="maxGameInstances">The max amount of instances of a game.</param>
    /// <param name="apiKey">games-service/game-instances-service api key.</param>
    /// <param name="preferredPlayerCapacity">The preferred capacity for servers.</param>
    /// <param name="placeVisitAccessKey">The access for place visits.</param>
    /// <param name="datacenterId">The ID of the host datacenter.</param>
    /// <param name="creatorId">The ID of the creator.</param>
    /// <param name="creatorType">The type of the creator.</param>
    /// <param name="placeVersion">The place version.</param>
    /// <param name="vipOwnerId">The vip server owner ID.</param>
    /// <param name="placeFetchUrl">The url to fetch the place with.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <param name="preferredPort">The preferred port.</param>
    public GameServerSettings(
        long placeId,
        long universeId,
        int matchmakingContextId,
        string jobSignature,
        string gameCode,
        string baseUrl,
        Guid gameId,
        string machineAddress,
        int serverId,
        int gsmInterval,
        int maxPlayers, 
        int maxGameInstances,
        string apiKey,
        int preferredPlayerCapacity, 
        string placeVisitAccessKey,
        int datacenterId,
        long creatorId,
        string creatorType,
        int placeVersion,
        long? vipOwnerId,
        string placeFetchUrl,
        string metadata,
        int? preferredPort
    )
    {
        PlaceId = placeId;
        UniverseId = universeId;
        MatchmakingContextId = matchmakingContextId;
        JobSignature = jobSignature;
        GameCode = gameCode;
        BaseUrl = baseUrl;
        GameId = gameId;
        MachineAddress = machineAddress;
        ServerId = serverId;
        GsmInterval = gsmInterval;
        MaxPlayers = maxPlayers;
        MaxGameInstances = maxGameInstances;
        ApiKey = apiKey;
        PreferredPlayerCapacity = preferredPlayerCapacity;
        PlaceVisitAccessKey = placeVisitAccessKey;
        DatacenterId = datacenterId;
        CreatorId = creatorId;
        CreatorType = creatorType;
        PlaceVersion = placeVersion;
        VipOwnerId = vipOwnerId;
        PlaceFetchUrl = placeFetchUrl;
        Metadata = metadata;
        PreferredPort = preferredPort;
    }
}
