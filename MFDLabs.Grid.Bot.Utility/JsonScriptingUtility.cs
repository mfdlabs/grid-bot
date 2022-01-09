using System;
using System.Collections.Generic;
using System.Linq;
using MFDLabs.Grid.Commands;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

// ReSharper disable MemberCanBePrivate.Global

namespace MFDLabs.Grid.Bot.Utility
{
    public static class JsonScriptingUtility
    {
        // should this return the settings or the command?
        public static (string, ThumbnailSettings) GetThumbnailScript(ThumbnailCommandType type, params object[] args)
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

        public static (string, GameServerSettings) GetGameServerScript(long placeId,
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
            int? preferredPort)
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

        public static (string, ExecuteScriptGameServerSettings) GetGameServerExecutionScript(string type,
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
            int? preferredPort)
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
                "sitetest4.robloxlabs.com",
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

        public static (string, GameServerSettings) GetSharedGameServerScript(string jobId, long placeId, long universeId)
        {
            return GetGameServerScript(
                placeId,
                universeId,
                1,
                $"{jobId}-Sig2",
                null,
                "sitetest4.robloxlabs.com",
                Guid.NewGuid(),
                NetworkingGlobal.GetLocalIp(),
                1,
                200,
                10,
                1,
                NetworkingGlobal.GenerateUuidv4(),
                1,
                null,
                200,
                1,
                "User",
                1,
                null,
                $"https://assetdelivery.sitetest4.robloxlabs.com/v1/asset?id={placeId}",
                null,
                null
            );
        }
    }
}
