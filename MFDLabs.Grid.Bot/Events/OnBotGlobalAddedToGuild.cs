using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Analytics.Google;

namespace MFDLabs.Grid.Bot.Events
{
    internal static class OnBotGlobalAddedToGuild
    {
        public static Task Invoke(SocketGuild guild) 
            => GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("BotGlobal", "Added to guild", $"{guild.Name}@{guild.Id}");
    }
}
