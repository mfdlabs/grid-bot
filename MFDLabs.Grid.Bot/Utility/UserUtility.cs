using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MFDLabs.Abstractions;
using MFDLabs.Discord.RbxUsers.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Users.Client;
using MFDLabs.Users.Client.Models.Users;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class UserUtility : SingletonBase<UserUtility>
    {
        private readonly IUsersClient _SharedUsersClient = new UsersClient(
            StaticCounterRegistry.Instance,
            new UsersClientConfig(
                Settings.Singleton.UsersServiceRemoteURL,
                Settings.Singleton.UsersServiceMaxRedirects,
                Settings.Singleton.UsersServiceRequestTimeout,
                Settings.Singleton.UsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                Settings.Singleton.UsersServiceCircuitBreakerRetryInterval
            )
        );

        private readonly IRbxDiscordUsersClient _SharedDiscordUsersClient = new RbxDiscordUsersClient(
            StaticCounterRegistry.Instance,
            new RbxDiscordUsersClientConfig(
                Settings.Singleton.RbxDiscordUsersServiceRemoteURL,
                Settings.Singleton.RbxDiscordUsersServiceMaxRedirects,
                Settings.Singleton.RbxDiscordUsersServiceRequestTimeout,
                Settings.Singleton.RbxDiscordUsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                Settings.Singleton.RbxDiscordUsersServiceCircuitBreakerRetryInterval
            )
        );

        public async Task<long?> GetRobloxIDByIUserAsync(IUser user)
        {
            var result = await _SharedDiscordUsersClient.ResolveRobloxUserByIDAsync(user.Id, CancellationToken.None);
            if (result.Username == null) return null;
            return result.ID;
        }

        public long? GetRobloxIDByIUser(IUser user)
        {
            return GetRobloxIDByIUserAsync(user).GetAwaiter().GetResult();
        }

        public bool GetIsUserBanned(long id)
        {
            return GetIsUserBannedAsync(id).GetAwaiter().GetResult();
        }

        public async Task<bool> GetIsUserBannedAsync(long id)
        {
            var request = new MultiGetByUserIdRequest
            {
                ExcludeBannedUsers = !Settings.Singleton.UserUtilityShouldResolveBannedUsers,
                UserIds = new List<long> { id }
            };
            var response = await _SharedUsersClient.MultiGetUsersByIdsAsync(request);
            if (response.Data.Count == 0) return true;
            return false;
        }

        public long? GetUserIDByUsername(string username)
        {
            return GetUserIDByUsernameAsync(username).GetAwaiter().GetResult();
        }

        public async Task<long?> GetUserIDByUsernameAsync(string username)
        {
            var request = new MultiGetByUsernameRequest
            {
                ExcludeBannedUsers = !Settings.Singleton.UserUtilityShouldResolveBannedUsers,
                Usernames = new List<string> { username }
            };

            var response = await _SharedUsersClient.MultiGetUsersByUsernamesAsync(request);

            if (response.Data.Count == 0) return null;

            return response.Data.First().ID;
        }
    }
}
