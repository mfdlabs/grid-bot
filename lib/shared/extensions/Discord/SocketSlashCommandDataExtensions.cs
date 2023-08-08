#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketSlashCommandDataExtensions
    {
        public static SocketSlashCommandDataOption GetOptionByName(this SocketSlashCommandData data, string name)
            => (from opt in data.Options where opt.Name == name select opt).FirstOrDefault();

        public static SocketSlashCommandDataOption GetOptionByName(this SocketSlashCommandDataOption data, string name)
            => (from opt in data.Options where opt.Name == name select opt).FirstOrDefault();

        public static object GetOptionValue(this SocketSlashCommandData data, string name)
        {
            var opt = data.GetOptionByName(name);
            if (opt == null) return null;

            return opt.Value;
        }

        public static object GetOptionValue(this SocketSlashCommandDataOption data, string name)
        {
            var opt = data.GetOptionByName(name);
            if (opt == null) return null;

            return opt.Value;
        }

        public static SocketSlashCommandDataOption GetSubCommand(this SocketSlashCommandData data)
            => (from opt in data.Options where opt.Type == ApplicationCommandOptionType.SubCommand select opt).FirstOrDefault();
    }
}

#endif