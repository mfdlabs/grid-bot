namespace MFDLabs.Grid.Arbiter.Test.Component.Utility;

using Commands;
using Networking;
using Text.Extensions;

// ReSharper disable MemberCanBePrivate.Global

public static class JsonScriptingUtility
{
    public static (string, ExecuteScriptGameServerSettings) GetGameServerExecutionScript(
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
    {
        var settings = new ExecuteScriptGameServerSettings(
            type,
            arguments,
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
        );

        return (
            new ExecuteScriptGameServerCommand(
                settings
            ).ToJson(),
            settings
        );
    }

    public static (string, ExecuteScriptGameServerSettings) GetSharedGameServerExecutionScript(string type, params (string, object)[] arguments)
    {
        var args = new Dictionary<string, object>();
        if (arguments.Length > 0) args = arguments.ToDictionary(i => i.Item1, i => i.Item2);
        return GetGameServerExecutionScript(
            type,
            args,
            1818,
            777,
            1,
            NetworkingGlobal.GenerateUuidv4(),
            null,
            "roblox.com",
            Guid.NewGuid(),
            NetworkingGlobal.GetLocalIp(),
            1,
            1000,
            100,
            10,
            NetworkingGlobal.GenerateUuidv4(),
            10,
            null,
            251,
            1,
            "User",
            100,
            null,
            null,
            null,
            null
        );
    }
}
