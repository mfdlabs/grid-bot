using System;
using System.Collections.Generic;

namespace MFDLabs.Grid.Commands
{
    public class ExecuteScriptGameServerSettings : GameServerSettings
    {
        public string Type { get; set; }
        public IDictionary<string, object> Arguments { get; }

        public ExecuteScriptGameServerSettings(string type, IDictionary<string, object> arguments, long placeId, long universeId, int matchmakingContextId, string jobSignature, string gameCode, string baseUrl, Guid gameId, string machineAddress, int serverId, int gsmInterval, int maxPlayers, int maxGameInstances, string apiKey, int preferredPlayerCapacity, string placeVisitAccessKey, int datacenterId, long creatorId, string creatorType, int placeVersion, long? vipOwnerId, string placeFetchUrl, string metadata, int? preferredPort)
            : base(placeId, universeId, matchmakingContextId, jobSignature, gameCode, baseUrl, gameId, machineAddress, serverId, gsmInterval, maxPlayers, maxGameInstances, apiKey, preferredPlayerCapacity, placeVisitAccessKey, datacenterId, creatorId, creatorType, placeVersion, vipOwnerId, placeFetchUrl, metadata, preferredPort)
        {
            Type = type;
            Arguments = arguments;
        }
    }
}
