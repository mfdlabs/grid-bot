using MFDLabs.Abstractions;
using MFDLabs.Instrumentation;
using MFDLabs.Users.Client;
using MFDLabs.Users.Client.Models.Users;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class UserUtility : SingletonBase<UserUtility>
    {
        private readonly UsersClient _SharedUsersClient = new UsersClient(
            StaticCounterRegistry.Instance,
            new UsersClientConfig(
                Settings.Singleton.UsersServiceRemoteURL,
                Settings.Singleton.UsersServiceMaxRedirects,
                Settings.Singleton.UsersServiceRequestTimeout,
                Settings.Singleton.UsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                Settings.Singleton.UsersServiceCircuitBreakerRetryInterval
            )
        );

        public bool GetIsUserBanned(long id)
        {
            var request = new MultiGetByUserIdRequest
            {
                ExcludeBannedUsers = !Settings.Singleton.UserUtilityShouldResolveBannedUsers,
                UserIds = new List<long> { id }
            };

            var response = _SharedUsersClient.MultiGetUsersByIds(request);

            if (response.Data.Count == 0) return true;

            return false;
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
            var request = new MultiGetByUsernameRequest
            {
                ExcludeBannedUsers = !Settings.Singleton.UserUtilityShouldResolveBannedUsers,
                Usernames = new List<string> { username }
            };

            var response = _SharedUsersClient.MultiGetUsersByUsernames(request);

            if (response.Data.Count == 0) return null;

            return response.Data.First().ID;
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
