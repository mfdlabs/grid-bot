using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MFDLabs.Discord.RbxUsers.Client;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Http.Client;
using MFDLabs.Users.Client;
using MFDLabs.Threading.Extensions;
using MFDLabs.Users.Client.Models.Users;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class UserUtility
    {
        private static readonly IUsersClient SharedUsersClient = new UsersClient(
            PerfmonCounterRegistryProvider.Registry,
            new UsersClientConfig(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRemoteURL,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceMaxRedirects,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRequestTimeout,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceCircuitBreakerRetryInterval
            )
        );

        private static readonly IRbxDiscordUsersClient SharedDiscordUsersClient = new RbxDiscordUsersClient(
            PerfmonCounterRegistryProvider.Registry,
            new RbxDiscordUsersClientConfig(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceRemoteURL,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceMaxRedirects,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceRequestTimeout,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceMaxCircuitBreakerFailuresBeforeTrip,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RbxDiscordUsersServiceCircuitBreakerRetryInterval
            )
        );

        public static async Task<long?> GetRobloxIdByIUserAsync(IUser user)
        {
            try
            {
                var result = await SharedDiscordUsersClient.ResolveRobloxUserByIdAsync(user.Id, CancellationToken.None);
                if (result.Username == null) return null;
                return result.Id;
            }
            catch (HttpRequestFailedException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound) { return null; }
            catch (Exception ex) { global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true); return null; }
        }

        public static long? GetRobloxIdByIUser(IUser user) => GetRobloxIdByIUserAsync(user).Sync();

        public static bool GetIsUserBanned(long id) => GetIsUserBannedAsync(id).Sync();

        private static async Task<bool> GetIsUserBannedAsync(long id)
        {
            try
            {
                var request = new MultiGetByUserIdRequest
                {
                    ExcludeBannedUsers = !global::MFDLabs.Grid.Bot.Properties.Settings.Default.UserUtilityShouldResolveBannedUsers,
                    UserIds = new List<long> { id }
                };
                var response = await SharedUsersClient.MultiGetUsersByIdsAsync(request);
                return response.Data.Count == 0;
            } catch (Exception ex) { global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true); return false; }
        }

        public static long? GetUserIdByUsername(string username) => GetUserIdByUsernameAsync(username).Sync();

        private static async Task<long?> GetUserIdByUsernameAsync(string username)
        {
            var request = new MultiGetByUsernameRequest
            {
                ExcludeBannedUsers = !global::MFDLabs.Grid.Bot.Properties.Settings.Default.UserUtilityShouldResolveBannedUsers,
                Usernames = new List<string> { username }
            };

            var response = await SharedUsersClient.MultiGetUsersByUsernamesAsync(request);

            return response.Data.Count == 0 ? null : response.Data.First().Id;
        }
    }
}
