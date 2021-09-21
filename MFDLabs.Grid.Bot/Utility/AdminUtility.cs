using Discord;
using Discord.WebSocket;
using MFDLabs.Abstractions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class AdminUtility : SingletonBase<AdminUtility>
    {
        public IReadOnlyCollection<string> AllowedChannels
        {
            get { return (from channel in Settings.Singleton.AllowedChannels.Split(',') where !channel.IsNullOrEmpty() select channel).ToArray(); }
        }

        public IReadOnlyCollection<string> Admins
        {
            get { return (from user in Settings.Singleton.Admins.Split(',') where !user.IsNullOrEmpty() select user).ToArray(); }
        }

        public IReadOnlyCollection<string> PrivilagedUsers
        {
            get { return (from user in Settings.Singleton.HigherPrivilagedUsers.Split(',') where !user.IsNullOrEmpty() select user).ToArray(); }
        }

        public bool CheckIsUserOwner(IUser user)
        {
            return CheckIsUserOwner(user.Id);
        }

        public bool CheckIsUserOwner(ulong id)
        {
            return id == Settings.Singleton.BotOwnerID;
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
            return Admins.Contains(id);
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
            return PrivilagedUsers.Contains(id);
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
            return AllowedChannels.Contains(id);
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
    }
}
