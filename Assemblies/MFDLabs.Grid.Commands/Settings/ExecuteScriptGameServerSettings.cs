namespace MFDLabs.Grid.Commands;

using System;
using System.Collections.Generic;

/// <summary>
/// A version of <see cref="GameServerSettings"/> that is used for executing a script.
/// </summary>
public class ExecuteScriptGameServerSettings : GameServerSettings
{
    /// <summary>
    /// The type of script/name of the script.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The arguments to pass to the script.
    /// </summary>
    public IDictionary<string, object> Arguments { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ExecuteScriptGameServerSettings"/>
    /// </summary>
    /// <param name="type">The type of script/name of the script.</param>
    /// <param name="arguments">The arguments to pass to the script.</param>
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
    public ExecuteScriptGameServerSettings(
        string type,
        IDictionary<string, object> arguments,
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
        : base(
            placeId,
            universeId, 
            matchmakingContextId, 
            jobSignature, 
            gameCode, 
            baseUrl, 
            gameId, 
            machineAddress, 
            serverId, 
            gsmInterval, 
            maxPlayers, 
            maxGameInstances, 
            apiKey, 
            preferredPlayerCapacity, 
            placeVisitAccessKey, 
            datacenterId, 
            creatorId, 
            creatorType, 
            placeVersion, 
            vipOwnerId, 
            placeFetchUrl, 
            metadata, 
            preferredPort
        )
    {
        Type = type;
        Arguments = arguments;
    }
}
