/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class SocketCommandBaseExtensions
    {
        public static ulong GetGuildId(this SocketCommandBase socketCommand)
        {
            var guildId = socketCommand.Channel.Id;
            if (socketCommand.Channel is SocketGuildChannel channel) guildId = channel.Guild.Id;
            return guildId;
        }

        // There's not really a reason to this, considering that it's not ephemeral by default,
        // in the end this is really just for wrapping follow-ups, and for understanding the method easier.
        public static async Task RespondPublicAsync(
            this SocketCommandBase command,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        )
        {
            if (!command.HasResponded)
            {
                await command.RespondAsync(
                    text,
                    embeds,
                    isTts,
                    false,
                    new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                    component,
                    embed,
                    options
                );
                return;
            }

            await command.FollowupAsync(
                text,
                embeds,
                isTts,
                false,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                component,
                embed,
                options
            );
        }

        public static async Task RespondPublicPingAsync(
            this SocketCommandBase command,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => await command.RespondPublicAsync(text, true, embeds, isTts, component, embed, options);

        public static async Task RespondEphemeralAsync(
            this SocketCommandBase command,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        )
        {
            if (!command.HasResponded)
            {
                await command.RespondAsync(
                    text,
                    embeds,
                    isTts,
                    true,
                    new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                    component,
                    embed,
                    options
                );

                return;
            }

            await command.FollowupAsync(
                text,
                embeds,
                isTts,
                true,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                component,
                embed,
                options
            );
        }

        public static async Task RespondEphemeralPingAsync(
            this SocketCommandBase command,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => await command.RespondEphemeralAsync(text, true, embeds, isTts, component, embed, options);

        public static async Task<RestFollowupMessage> RespondWithFileEphemeralAsync(
            this SocketCommandBase command,
            Stream fileStream,
            string fileName,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent components = null,
            Embed embed = null,
            RequestOptions options = null
        )
        {
            return await command.FollowupWithFileAsync(
                fileStream,
                fileName,
                text,
                embeds,
                isTts,
                true,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                components,
                embed,
                options
            );
        }

        public static async Task<RestFollowupMessage> RespondWithFileEphemeralPingAsync(
            this SocketCommandBase command,
            Stream fileStream,
            string fileName,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent components = null,
            Embed embed = null,
            RequestOptions options = null
        ) => await command.RespondWithFileEphemeralAsync(fileStream, fileName, text, true, embeds, isTts, components, embed, options);

        public static async Task<RestFollowupMessage> RespondWithFilePublicAsync(
           this SocketCommandBase command,
           Stream fileStream,
           string fileName,
           string text = null,
           bool pingUser = false,
           Embed[] embeds = null,
           bool isTts = false,
           MessageComponent components = null,
           Embed embed = null,
           RequestOptions options = null
        )
        {
            return await command.FollowupWithFileAsync(
                fileStream,
                fileName,
                text,
                embeds,
                isTts,
                false,
                new AllowedMentions(AllowedMentionTypes.Users) { MentionRepliedUser = pingUser },
                components,
                embed,
                options
            );
        }

        public static async Task<RestFollowupMessage> RespondWithFilePublicPingAsync(
           this SocketCommandBase command,
           Stream fileStream,
           string fileName,
           string text = null,
           Embed[] embeds = null,
           bool isTts = false,
           MessageComponent components = null,
           Embed embed = null,
           RequestOptions options = null
        ) => await command.RespondWithFilePublicAsync(fileStream, fileName, text, true, embeds, isTts, components, embed, options);


        public static RestFollowupMessage RespondWithFileEphemeral(
            this SocketCommandBase command,
            Stream fileStream,
            string fileName,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent components = null,
            Embed embed = null,
            RequestOptions options = null
        ) => command.RespondWithFileEphemeralAsync(
                fileStream,
                fileName,
                text,
                pingUser,
                embeds,
                isTts,
                components,
                embed,
                options
            )
            .GetAwaiter()
            .GetResult();

        public static RestFollowupMessage RespondWithFileEphemeralPing(
            this SocketCommandBase command,
            Stream fileStream,
            string fileName,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent components = null,
            Embed embed = null,
            RequestOptions options = null
        ) => command.RespondWithFileEphemeralPingAsync(
                fileStream,
                fileName,
                text,
                embeds,
                isTts,
                components,
                embed,
                options
            )
            .GetAwaiter()
            .GetResult();

        public static RestFollowupMessage RespondWithFilePublic(
           this SocketCommandBase command,
           Stream fileStream,
           string fileName,
           string text = null,
           bool pingUser = false,
           Embed[] embeds = null,
           bool isTts = false,
           MessageComponent components = null,
           Embed embed = null,
           RequestOptions options = null
       ) => command.RespondWithFilePublicAsync(
               fileStream,
               fileName,
               text,
               pingUser,
               embeds,
               isTts,
               components,
               embed,
               options
           )
           .GetAwaiter()
           .GetResult();

        public static RestFollowupMessage RespondWithFilePublicPing(
           this SocketCommandBase command,
           Stream fileStream,
           string fileName,
           string text = null,
           Embed[] embeds = null,
           bool isTts = false,
           MessageComponent components = null,
           Embed embed = null,
           RequestOptions options = null
       ) => command.RespondWithFilePublicPingAsync(
               fileStream,
               fileName,
               text,
               embeds,
               isTts,
               components,
               embed,
               options
           )
           .GetAwaiter()
           .GetResult();

        public static void RespondEphemeral(
            this SocketCommandBase command,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => command.RespondEphemeralAsync(text, pingUser, embeds, isTts, component, embed, options).GetAwaiter().GetResult();

        public static void RespondEphemeralPing(
            this SocketCommandBase command,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => command.RespondEphemeralPingAsync(text, embeds, isTts, component, embed, options).GetAwaiter().GetResult();

        public static void RespondPublic(
            this SocketCommandBase command,
            string text = null,
            bool pingUser = false,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => command.RespondPublicAsync(text, pingUser, embeds, isTts, component, embed, options).GetAwaiter().GetResult();

        public static void RespondPublicPing(
            this SocketCommandBase command,
            string text = null,
            Embed[] embeds = null,
            bool isTts = false,
            MessageComponent component = null,
            Embed embed = null,
            RequestOptions options = null
        ) => command.RespondPublicPingAsync(text, embeds, isTts, component, embed, options).GetAwaiter().GetResult();

        public static async Task<bool> RejectIfNotAdminAsync(this SocketCommandBase cmd)
            => await AdminUtility.RejectIfNotAdminAsync(cmd);
        public static async Task<bool> RejectIfNotPrivilagedAsync(this SocketCommandBase cmd)
            => await AdminUtility.RejectIfNotPrivilagedAsync(cmd);
        public static async Task<bool> RejectIfNotOwnerAsync(this SocketCommandBase cmd)
            => await AdminUtility.RejectIfNotOwnerAsync(cmd);
        public static bool RejectIfNotPrivilaged(this SocketCommandBase cmd)
            => cmd.RejectIfNotPrivilagedAsync().GetAwaiter().GetResult();
        public static bool RejectIfNotAdmin(this SocketCommandBase cmd)
            => cmd.RejectIfNotAdminAsync().GetAwaiter().GetResult();
        public static bool RejectIfNotOwner(this SocketCommandBase cmd)
            => cmd.RejectIfNotOwnerAsync().GetAwaiter().GetResult();

        internal class DiscardDisposable : IDisposable
        {
            public static DiscardDisposable Instance = new();

            public void Dispose() { }
        }

        public static async Task<IDisposable> DeferEphemeralAsync(this SocketCommandBase command, RequestOptions options = null)
        {
            await command.DeferAsync(true, options);
            return DiscardDisposable.Instance;
        }

        public static IDisposable DeferEphemeral(this SocketCommandBase command, RequestOptions options = null)
            => command.DeferEphemeralAsync(options).GetAwaiter().GetResult();

        public static async Task<IDisposable> DeferPublicAsync(this SocketCommandBase command, RequestOptions options = null)
        {
            await command.DeferAsync(false, options);
            return DiscardDisposable.Instance;
        }

        public static IDisposable DeferPublic(this SocketCommandBase command, RequestOptions options = null)
            => command.DeferPublicAsync(options).GetAwaiter().GetResult();
    }
}

#endif