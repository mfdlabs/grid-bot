namespace Grid.Bot.Utility;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Users.Client;

/// <summary>
/// Utility class for interacting with the Roblox Users API.
/// </summary>
public class RbxUsersUtility : IRbxUsersUtility
{
    private readonly IUsersClient _usersClient;

    /// <summary>
    /// Construct a new instance of <see cref="RbxUsersUtility"/>.
    /// </summary>
    /// <param name="usersClient">The <see cref="IUsersClient"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="usersClient"/> cannot be null.</exception>
    public RbxUsersUtility(IUsersClient usersClient)
    {
        _usersClient = usersClient ?? throw new ArgumentNullException(nameof(usersClient));
    }

    /// <inheritdoc cref="IRbxUsersUtility.GetIsUserBannedAsync(long)"/>
    public async Task<bool> GetIsUserBannedAsync(long id)
    {
        try
        {
            var request = new MultiGetByUserIdRequest
            {
                ExcludeBannedUsers = false,
                UserIds = new List<long> { id }
            };

            var response = await _usersClient.MultiGetUsersByIdsAsync(request);
            return response.Data.Count == 0;
        }
        catch 
        {
            return false;
        }
    }

    /// <inheritdoc cref="IRbxUsersUtility.GetUserIdByUsernameAsync(string)"/>
    public async Task<long?> GetUserIdByUsernameAsync(string username)
    {
        var request = new MultiGetByUsernameRequest
        {
            ExcludeBannedUsers = false,
            Usernames = new List<string> { username }
        };

        var response = await _usersClient.MultiGetUsersByUsernamesAsync(request);

        return response.Data.First()?.Id;
    }
}
