using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MFDLabs.Abstractions;
using MFDLabs.Discord.RbxUsers.Client;
using MFDLabs.Http.Client;
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
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRemoteURL,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceMaxRedirects,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRequestTimeout,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceCircuitBreakerRetryInterval
            )
        );

        private readonly IRbxDiscordUsersClient _SharedDiscordUsersClient = new RbxDiscordUsersClient(
            StaticCounterRegistry.Instance,
            new RbxDiscordUsersClientConfig(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceRemoteURL,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceMaxRedirects,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceRequestTimeout,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceCircuitBreakerRetryInterval
            )
        );

        public async Task<long?> GetRobloxIDByIUserAsync(IUser user)
        {
            try
            {
                var result = await _SharedDiscordUsersClient.ResolveRobloxUserByIDAsync(user.Id, CancellationToken.None);
                if (result.Username == null) return null;
                return result.ID;
            }
            catch (HttpRequestFailedException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound) { return null; }
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
            try
            {
                var request = new MultiGetByUserIdRequest
                {
                    ExcludeBannedUsers = !global::MFDLabs.Grid.Bot.Properties.Settings.Default.UserUtilityShouldResolveBannedUsers,
                    UserIds = new List<long> { id }
                };
                var response = await _SharedUsersClient.MultiGetUsersByIdsAsync(request);
                if (response.Data.Count == 0) return true;
                return false;
            } catch { return false; }
        }

        public long? GetUserIDByUsername(string username)
        {
            return GetUserIDByUsernameAsync(username).GetAwaiter().GetResult();
        }

        public async Task<long?> GetUserIDByUsernameAsync(string username)
        {
            var request = new MultiGetByUsernameRequest
            {
                ExcludeBannedUsers = !global::MFDLabs.Grid.Bot.Properties.Settings.Default.UserUtilityShouldResolveBannedUsers,
                Usernames = new List<string> { username }
            };

            var response = await _SharedUsersClient.MultiGetUsersByUsernamesAsync(request);

            if (response.Data.Count == 0) return null;

            return response.Data.First().ID;
        }
    }
}
