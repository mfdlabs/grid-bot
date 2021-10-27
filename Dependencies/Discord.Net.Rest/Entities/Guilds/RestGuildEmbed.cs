using System.Diagnostics;
using Model = Discord.API.GuildEmbed;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public struct RestGuildEmbed
    {
        public bool IsEnabled { get; private set; }
        public ulong? ChannelId { get; private set; }

        internal RestGuildEmbed(bool isEnabled, ulong? channelId)
        {
            ChannelId = channelId;
            IsEnabled = isEnabled;
        }
        internal static RestGuildEmbed Create(Model model)
        {
            return new RestGuildEmbed(model.Enabled, model.ChannelId);
        }

        public override string ToString() => ChannelId?.ToString() ?? "Unknown";
        private string DebuggerDisplay => $"{ChannelId} ({(IsEnabled ? "Enabled" : "Disabled")})";
    }
}
