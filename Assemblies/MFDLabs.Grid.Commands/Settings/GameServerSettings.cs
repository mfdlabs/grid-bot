using System;

namespace MFDLabs.Grid.Commands
{
    public class GameServerSettings
    {
        public long PlaceId { get; }
        public long UniverseId { get; }
        public int MatchmakingContextId { get; }
        public string JobSignature { get; }
        public string GameCode { get; }
        public string BaseUrl { get; }
        public Guid GameId { get; }
        public string MachineAddress { get; }
        public int ServerId { get; }
        public int GsmInterval { get; }
        public int MaxPlayers { get; }
        public int MaxGameInstances { get; }
        public string ApiKey { get; }
        public int PreferredPlayerCapacity { get; }
        public string PlaceVisitAccessKey { get; }
        public int DatacenterId { get; }
        public long CreatorId { get; }
        public string CreatorType { get; }
        public int PlaceVersion { get; }
        public long? VipOwnerId { get; }
        public string PlaceFetchUrl { get; }
        public string Metadata { get; }
        public int? PreferredPort { get; }

        public GameServerSettings(long placeId, long universeId, int matchmakingContextId, string jobSignature, string gameCode, string baseUrl, Guid gameId, string machineAddress, int serverId, int gsmInterval, int maxPlayers, int maxGameInstances, string apiKey, int preferredPlayerCapacity, string placeVisitAccessKey, int datacenterId, long creatorId, string creatorType, int placeVersion, long? vipOwnerId, string placeFetchUrl, string metadata, int? preferredPort)
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
}