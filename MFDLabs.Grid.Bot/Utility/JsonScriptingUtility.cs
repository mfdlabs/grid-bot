using System;
using System.Collections.Generic;
using MFDLabs.Abstractions;
using MFDLabs.Grid.Commands;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class JsonScriptingUtility : SingletonBase<JsonScriptingUtility>
    {
        // should this return the settings or the command?
        public (string, ThumbnailSettings) GetThumbnailScript(ThumbnailCommandType type, params object[] args)
        {
            var settings = new ThumbnailSettings(
                type,
                args
            );

            return (
                new ThumbnailCommand(
                    settings
                ).ToJson(),
                settings
            );
        }

        public (string, GameServerSettings) GetGameServerScript(long placeId, long universeId, int matchmakingContextId, string jobSignature, string gameCode, string baseUrl, Guid gameId, string machineAddress, int serverId, int gsmInterval, int maxPlayers, int maxGameInstances, string apiKey, int preferredPlayerCapacity, string placeVisitAccessKey, int datacenterId, long creatorId, string creatorType, int placeVersion, long? vipOwnerId, string placeFetchUrl, string metadata, int? preferredPort)
        {
            var settings = new GameServerSettings(
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
                new GameServerCommand(
                    settings
                ).ToJson(),
                settings
            );
        }

        public (string, ExecuteScriptGameServerSettings) GetGameServerExecutionScript(string type, IDictionary<string, object> arguments, long placeId, long universeId, int matchmakingContextId, string jobSignature, string gameCode, string baseUrl, Guid gameId, string machineAddress, int serverId, int gsmInterval, int maxPlayers, int maxGameInstances, string apiKey, int preferredPlayerCapacity, string placeVisitAccessKey, int datacenterId, long creatorId, string creatorType, int placeVersion, long? vipOwnerId, string placeFetchUrl, string metadata, int? preferredPort)
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

        public (string, ExecuteScriptGameServerSettings) GetSharedGameServerExecutionScript(string type, IDictionary<string, object> arguments)
        {
            return GetGameServerExecutionScript(
                type,
                arguments,
                1818,
                777,
                1,
                NetworkingGlobal.Singleton.GenerateUUIDV4(),
                null,
                "sitetest4.robloxlabs.com",
                Guid.NewGuid(),
                NetworkingGlobal.Singleton.GetLocalIP(),
                1,
                1000,
                100,
                10,
                NetworkingGlobal.Singleton.GenerateUUIDV4(),
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

        public (string, GameServerSettings) GetSharedGameServerScript(string jobID, long placeID, long universeID)
        {
            return GetGameServerScript(
                placeID,
                universeID,
                1,
                $"{jobID}-Sig2",
                null,
                "sitetest4.robloxlabs.com",
                Guid.NewGuid(),
                NetworkingGlobal.Singleton.GetLocalIP(),
                1,
                200,
                10,
                1,
                NetworkingGlobal.Singleton.GenerateUUIDV4(),
                1,
                null,
                200,
                1,
                "User",
                1,
                null,
                $"https://assetdelivery.sitetest4.robloxlabs.com/v1/asset?id={placeID}",
                null,
                null
            );
        }
    }
}
