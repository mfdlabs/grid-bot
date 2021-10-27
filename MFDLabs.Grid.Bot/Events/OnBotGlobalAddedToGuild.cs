using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Analytics.Google;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnBotGlobalAddedToGuild
    {
        public static Task Invoke(SocketGuild guild) => Manager.Singleton.TrackNetworkEventAsync("BotGlobal", "Added to guild", $"{guild.Name}@{guild.Id}");
    }
}
