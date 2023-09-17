namespace Grid.Bot.Utility;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Users.Client;
using Threading.Extensions;

/// <summary>
/// Utility class for interacting with the Roblox Users API.
/// </summary>
public static class RbxUsersUtility
{
    private static readonly IUsersClient SharedUsersClient = new UsersClient(
        UsersClientSettings.Singleton.UsersApiBaseUrl
    );

    /// <summary>
    /// Gets the banned status of a user.
    /// </summary>
    /// <param name="id">The ID of the Roblox user.</param>
    /// <returns>True if the user is banned, false otherwise.</returns>
    public static bool GetIsUserBanned(long id) => GetIsUserBannedAsync(id).Sync();

    private static async Task<bool> GetIsUserBannedAsync(long id)
    {
        try
        {
            var request = new MultiGetByUserIdRequest
            {
                ExcludeBannedUsers = false,
                UserIds = new List<long> { id }
            };

            var response = await SharedUsersClient.MultiGetUsersByIdsAsync(request);
            return response.Data.Count == 0;
        }
        catch (Exception ex)
        {
            BacktraceUtility.UploadCrashLog(ex);
            return false;
        }
    }

    /// <summary>
    /// Gets the ID of the specified Roblox User.
    /// </summary>
    /// <param name="username">The name of the user.</param>
    /// <returns>The ID of the user.</returns>
    public static long? GetUserIdByUsername(string username) => GetUserIdByUsernameAsync(username).Sync();

    private static async Task<long?> GetUserIdByUsernameAsync(string username)
    {
        var request = new MultiGetByUsernameRequest
        {
            ExcludeBannedUsers = false,
            Usernames = new List<string> { username }
        };

        var response = await SharedUsersClient.MultiGetUsersByUsernamesAsync(request);

        return response.Data.First()?.Id;
    }
}
