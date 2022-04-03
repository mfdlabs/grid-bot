/* Copyright MFDLABS Corporation. All rights reserved. */

using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Analytics.Google;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnBotGlobalAddedToGuild
    {
        public static Task Invoke(SocketGuild guild) 
            => GoogleAnalyticsManager.TrackNetworkEventAsync("BotGlobal", "Added to guild", $"{guild.Name}@{guild.Id}");
    }
}
