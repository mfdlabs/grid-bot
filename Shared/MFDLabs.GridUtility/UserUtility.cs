using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using MFDLabs.Users.Client;
using MFDLabs.Threading.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class UserUtility
    {
        private static readonly IUsersClient SharedUsersClient = new UsersClient(
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.UsersServiceRemoteURL
        );

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
