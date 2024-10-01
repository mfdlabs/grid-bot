namespace Grid.Bot.Utility;

using System.Threading.Tasks;

/// <summary>
/// Utility class for interacting with the Roblox Users API.
/// </summary>
public interface IRbxUsersUtility
{
    /// <summary>
    /// Gets the banned status of a user.
    /// </summary>
    /// <param name="id">The ID of the Roblox user.</param>
    /// <returns>True if the user is banned, false otherwise.</returns>
    Task<bool> GetIsUserBannedAsync(long id);

    /// <summary>
    /// Gets the ID of the specified Roblox User.
    /// </summary>
    /// <param name="username">The name of the user.</param>
    /// <returns>The ID of the user.</returns>
    Task<long?> GetUserIdByUsernameAsync(string username);
}
