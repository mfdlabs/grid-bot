using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Abstractions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class AdminUtility : SingletonBase<AdminUtility>
    {
        public IReadOnlyCollection<string> AllowedChannelIDs
        {
            get { return (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowedChannels.Split(',') where !id.IsNullOrEmpty() select id).ToArray(); }
        }

        public IReadOnlyCollection<string> AdministratorUserIDs
        {
            get { return (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.Admins.Split(',') where !id.IsNullOrEmpty() select id).ToArray(); }
        }

        public IReadOnlyCollection<string> HigherPrivilagedUserIDs
        {
            get { return (from id in global::MFDLabs.Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers.Split(',') where !id.IsNullOrEmpty() select id).ToArray(); }
        }

        public bool UserIsOwner(IUser user)
        {
            return UserIsOwner(user.Id);
        }

        public bool UserIsOwner(ulong id)
        {
            return id == global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID;
        }

        public bool UserIsAdmin(IUser user)
        {
            return UserIsAdmin(user.Id);
        }

        public bool UserIsAdmin(ulong id)
        {
            return UserIsAdmin(id.ToString());
        }

        public bool UserIsAdmin(string id)
        {
            if (UserIsOwner(ulong.Parse(id))) return true;
            return AdministratorUserIDs.Contains(id);
        }

        public bool UserIsPrivilaged(IUser user)
        {
            return UserIsPrivilaged(user.Id);
        }

        public bool UserIsPrivilaged(ulong id)
        {
            return UserIsPrivilaged(id.ToString());
        }

        public bool UserIsPrivilaged(string id)
        {
            if (UserIsAdmin(id)) return true;
            return HigherPrivilagedUserIDs.Contains(id);
        }

        public bool ChannelIsAllowed(IChannel channel)
        {
            return ChannelIsAllowed(channel.Id);
        }

        public bool ChannelIsAllowed(ulong id)
        {
            return ChannelIsAllowed(id.ToString());
        }

        public bool ChannelIsAllowed(string id)
        {
            return AllowedChannelIDs.Contains(id);
        }

        public async Task<bool> RejectIfNotPrivilagedAsync(SocketMessage message)
        {
            var isPrivilaged = UserIsPrivilaged(message.Author);
            var isAdmin = UserIsAdmin(message.Author);

            if (!isPrivilaged && !isAdmin)
            {
                SystemLogger.Singleton.Warning("User '{0}' is not on the admin whitelist or the privilaged users list. Please take this with caution as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("Only privilaged users or administrators can execute that command.");
                return false;
            }

            SystemLogger.Singleton.Info("User '{0}' is privilaged or an admin.", message.Author.Id);
            return true;
        }

        public async Task<bool> RejectIfNotAdminAsync(SocketMessage message)
        {
            if (!UserIsAdmin(message.Author))
            {
                SystemLogger.Singleton.Warning("User '{0}' is not on the admin whitelist. Please take this with caution as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("You lack the correct permissions to execute that command.");
                return false;
            }
            SystemLogger.Singleton.Info("User '{0}' is on the admin whitelist.", message.Author.Id);
            return true;
        }

        public async Task<bool> RejectIfNotOwnerAsync(SocketMessage message)
        {
            if (!UserIsOwner(message.Author))
            {
                SystemLogger.Singleton.Warning("User '{0}' is not the owner. Please take this with caution as leaked internal methods may be abused!", message.Author.Id);
                await message.ReplyAsync("You lack the correct permissions to execute that command.");
                return false;
            }
            SystemLogger.Singleton.Info("User '{0}' is the owner.", message.Author.Id);
            return true;
        }
    }
}
