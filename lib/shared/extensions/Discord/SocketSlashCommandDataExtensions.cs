#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.Extensions;

using System;
using System.Linq;

using Discord;
using Discord.WebSocket;

/// <summary>
/// Extension methods for <see cref="SocketSlashCommandData"/> and <see cref="SocketSlashCommandDataOption"/>.
/// </summary>
public static class SocketSlashCommandDataExtensions
{
    private static SocketSlashCommandDataOption GetOptionByName(this SocketSlashCommandData data, string name)
        => (from opt in data.Options where opt.Name == name select opt).FirstOrDefault();

    private static SocketSlashCommandDataOption GetOptionByName(this SocketSlashCommandDataOption data, string name)
        => (from opt in data.Options where opt.Name == name select opt).FirstOrDefault();

    /// <summary>
    /// Gets the value of an option in <see cref="SocketSlashCommandData"/>
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="data">The <see cref="SocketSlashCommandData"/></param>
    /// <param name="name">The name of the option.</param>
    /// <param name="default">The default value.</param>
    /// <returns>The value of the option.</returns>
    public static T GetOptionValue<T>(this SocketSlashCommandData data, string name, T @default = default(T))
    {
        var opt = data.GetOptionByName(name);
        if (opt == null) return @default;

        return (T)Convert.ChangeType(opt.Value, typeof(T));
    }

    /// <summary>
    /// Gets the value of an option in <see cref="SocketSlashCommandDataOption"/>
    /// </summary>
    /// <typeparam name="T">The type of the argument.</typeparam>
    /// <param name="data">The <see cref="SocketSlashCommandDataOption"/></param>
    /// <param name="name">The name of the option.</param>
    /// <param name="default">The default value.</param>
    /// <returns>The value of the option.</returns>
    public static T GetOptionValue<T>(this SocketSlashCommandDataOption data, string name, T @default = default(T))
    {
        var opt = data.GetOptionByName(name);
        if (opt == null) return @default;

        return (T)Convert.ChangeType(opt.Value, typeof(T));
    }

    /// <summary>
    /// Gets the sub command off the <see cref="SocketSlashCommandData"/>
    /// </summary>
    /// <param name="data">The <see cref="SocketSlashCommandData"/></param>
    /// <returns>The sub command.</returns>
    public static SocketSlashCommandDataOption GetSubCommand(this SocketSlashCommandData data)
        => (from opt in data.Options where opt.Type == ApplicationCommandOptionType.SubCommand select opt).FirstOrDefault();
}

#endif
