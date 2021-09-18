using MFDLabs.Http;
using MFDLabs.Users.Client.Enumeration;
using MFDLabs.Users.Client.Models.Usernames;
using MFDLabs.Users.Client.Models.Users;
using MFDLabs.Users.Client.Models.UserSearch;
using MFDLabs.Users.Client.Models.WebAPI;
using System.Threading.Tasks;

namespace MFDLabs.Users.Client
{
    public interface IUsersClient
    {
        /// <summary>Validate a display name for a new user.</summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="birthdate">The new user's birthdate</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<ApiEmptyResponseModel> ValidateNewUserDisplayNameAsync(string displayName, string birthdate);

        /// <summary>Validate a display name for a new user.</summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="birthdate">The new user's birthdate</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        ApiEmptyResponseModel ValidateNewUserDisplayName(string displayName, string birthdate);

        /// <summary>Gets detailed user information by id.</summary>
        /// <param name="userId">The user id.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<UserResponseV2> GetUserDetailsAsync(long userId);

        /// <summary>Gets detailed user information by id.</summary>
        /// <param name="userId">The user id.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        UserResponseV2 GetUserDetails(long userId);

        /// <summary>Gets a user's status.</summary>
        /// <param name="userId">The user id.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<UserStatusResponse> GetUserStatusAsync(long userId);

        /// <summary>Gets a user's status.</summary>
        /// <param name="userId">The user id.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        UserStatusResponse GetUserStatus(long userId);

        /// <summary>Retrieves the username history for a particular user.</summary>
        /// <param name="limit">The amount of results per request.</param>
        /// <param name="cursor">The paging cursor for the previous or next page.</param>
        /// <param name="sortOrder">The order the results are sorted in.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<ApiPageResponse<UsernameHistoryResponse>> GetUsernameHistoryAsync(long userId, int? limit, string cursor, SortOrder? sortOrder);

        /// <summary>Retrieves the username history for a particular user.</summary>
        /// <param name="limit">The amount of results per request.</param>
        /// <param name="cursor">The paging cursor for the previous or next page.</param>
        /// <param name="sortOrder">The order the results are sorted in.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        ApiPageResponse<UsernameHistoryResponse> GetUsernameHistory(long userId, int? limit, string cursor, SortOrder? sortOrder);

        /// <summary>Searches for users by keyword.</summary>
        /// <param name="keyword">The search keyword.</param>
        /// <param name="limit">The amount of results per request.</param>
        /// <param name="cursor">The paging cursor for the previous or next page.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<ApiPageResponse<UserSearchUserResponse>> SearchUsersAsync(string keyword, int? limit, string cursor);

        /// <summary>Searches for users by keyword.</summary>
        /// <param name="keyword">The search keyword.</param>
        /// <param name="limit">The amount of results per request.</param>
        /// <param name="cursor">The paging cursor for the previous or next page.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        ApiPageResponse<UserSearchUserResponse> SearchUsers(string keyword, int? limit, string cursor);

        /// <summary>Get users by usernames.</summary>
        /// <param name="request">The {Roblox.Users.Api.MultiGetByUsernameRequest}.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<ApiArrayResponse<MultiGetUserByNameResponse>> MultiGetUsersByUsernamesAsync(MultiGetByUsernameRequest request);

        /// <summary>Get users by usernames.</summary>
        /// <param name="request">The {Roblox.Users.Api.MultiGetByUsernameRequest}.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        ApiArrayResponse<MultiGetUserByNameResponse> MultiGetUsersByUsernames(MultiGetByUsernameRequest request);

        /// <summary>Get users by ids.</summary>
        /// <param name="request">The {Roblox.Users.Api.MultiGetByUserIdRequest}.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        Task<ApiArrayResponse<SkinnyUserResponse>> MultiGetUsersByIdsAsync(MultiGetByUserIdRequest request);

        /// <summary>Get users by ids.</summary>
        /// <param name="request">The {Roblox.Users.Api.MultiGetByUserIdRequest}.</param>
        /// <returns>OK</returns>
        /// <exception cref="HttpException">A server side error occurred.</exception>
        ApiArrayResponse<SkinnyUserResponse> MultiGetUsersByIds(MultiGetByUserIdRequest request);
    }
}
