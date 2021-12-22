using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;
using MFDLabs.Users.Client.Enumeration;
using MFDLabs.Users.Client.Models.Usernames;
using MFDLabs.Users.Client.Models.Users;
using MFDLabs.Users.Client.Models.UserSearch;
using MFDLabs.Users.Client.Models.WebAPI;

namespace MFDLabs.Users.Client
{
    public class UsersClient : IUsersClient
    {
        private readonly IHttpRequestSender _requestSender;

        public UsersClient(ICounterRegistry counterRegistry, UsersClientConfig config)
        {
            var settings = new UsersClientSettings(config);
            _requestSender = new HttpRequestSender(new UsersHttpClientBuilder(counterRegistry,
                    settings,
                    config).Build(),
                new HttpRequestBuilder(settings.Endpoint));
        }

        public UserResponseV2 GetUserDetails(long userId)
        {
            return _requestSender.SendRequest<UserResponseV2>(HttpMethod.Get,
                $"/v1/users/{userId}");
        }

        public Task<UserResponseV2> GetUserDetailsAsync(long userId)
        {
            return _requestSender.SendRequestAsync<UserResponseV2>(HttpMethod.Get,
                $"/v1/users/{userId}",
                CancellationToken.None);
        }

        public ApiPageResponse<UsernameHistoryResponse> GetUsernameHistory(long userId,
            int? limit,
            string cursor,
            SortOrder? sortOrder)
        {
            return _requestSender.SendRequest<ApiPageResponse<UsernameHistoryResponse>>(HttpMethod.Get,
                $"/v1/users/{userId}/username-history",
                BuildQueryStringForPaginationRequest(limit,
                    cursor,
                    sortOrder));
        }

        public Task<ApiPageResponse<UsernameHistoryResponse>> GetUsernameHistoryAsync(long userId, int? limit,
            string cursor, SortOrder? sortOrder)
        {
            return _requestSender.SendRequestAsync<ApiPageResponse<UsernameHistoryResponse>>(HttpMethod.Get,
                $"/v1/users/{userId}/username-history", CancellationToken.None,
                BuildQueryStringForPaginationRequest(limit, cursor, sortOrder));
        }

        public UserStatusResponse GetUserStatus(long userId)
        {
            return _requestSender.SendRequest<UserStatusResponse>(HttpMethod.Get, $"/v1/users/{userId}/status");
        }

        public Task<UserStatusResponse> GetUserStatusAsync(long userId)
        {
            return _requestSender.SendRequestAsync<UserStatusResponse>(HttpMethod.Get, $"/v1/users/{userId}/status",
                CancellationToken.None);
        }

        public ApiArrayResponse<SkinnyUserResponse> MultiGetUsersByIds(MultiGetByUserIdRequest request)
        {
            return _requestSender
                .SendRequestWithJsonBody<MultiGetByUserIdRequest, ApiArrayResponse<SkinnyUserResponse>>(HttpMethod.Post,
                    "/v1/users", request);
        }

        public Task<ApiArrayResponse<SkinnyUserResponse>> MultiGetUsersByIdsAsync(MultiGetByUserIdRequest request)
        {
            return _requestSender
                .SendRequestWithJsonBodyAsync<MultiGetByUserIdRequest, ApiArrayResponse<SkinnyUserResponse>>(
                    HttpMethod.Post, "/v1/users", request, CancellationToken.None);
        }

        public ApiArrayResponse<MultiGetUserByNameResponse> MultiGetUsersByUsernames(MultiGetByUsernameRequest request)
        {
            return _requestSender
                .SendRequestWithJsonBody<MultiGetByUsernameRequest, ApiArrayResponse<MultiGetUserByNameResponse>>(
                    HttpMethod.Post, "/v1/usernames/users", request);
        }

        public Task<ApiArrayResponse<MultiGetUserByNameResponse>> MultiGetUsersByUsernamesAsync(
            MultiGetByUsernameRequest request)
        {
            return _requestSender
                .SendRequestWithJsonBodyAsync<MultiGetByUsernameRequest, ApiArrayResponse<MultiGetUserByNameResponse>>(
                    HttpMethod.Post, "/v1/usernames/users", request, CancellationToken.None);
        }

        public ApiPageResponse<UserSearchUserResponse> SearchUsers(string keyword, int? limit, string cursor)
        {
            if (keyword.IsNullOrEmpty()) throw new ArgumentNullException(nameof(keyword));

            return _requestSender.SendRequest<ApiPageResponse<UserSearchUserResponse>>(HttpMethod.Get,
                "/v1/users/search", BuildQueryStringForSearchRequest(keyword, limit, cursor));
        }

        public Task<ApiPageResponse<UserSearchUserResponse>> SearchUsersAsync(string keyword, int? limit, string cursor)
        {
            if (keyword.IsNullOrEmpty()) throw new ArgumentNullException(nameof(keyword));

            return _requestSender.SendRequestAsync<ApiPageResponse<UserSearchUserResponse>>(HttpMethod.Get,
                "/v1/users/search", CancellationToken.None, BuildQueryStringForSearchRequest(keyword, limit, cursor));
        }

        public ApiEmptyResponseModel ValidateNewUserDisplayName(string displayName, string birthdate)
        {
            if (!DateTime.TryParse(birthdate, out _))
                throw new ApplicationException("Invalid birthdate for display name validation request.");

            return _requestSender.SendRequest<ApiEmptyResponseModel>(HttpMethod.Get, "/v1/display-names/validate",
                BuildQueryStringForDisplayNameValidationRequest(displayName, birthdate));
        }

        public Task<ApiEmptyResponseModel> ValidateNewUserDisplayNameAsync(string displayName, string birthdate)
        {
            if (!DateTime.TryParse(birthdate, out _))
                throw new ApplicationException("Invalid birthdate for display name validation request.");

            return _requestSender.SendRequestAsync<ApiEmptyResponseModel>(HttpMethod.Get, "/v1/display-names/validate",
                CancellationToken.None, BuildQueryStringForDisplayNameValidationRequest(displayName, birthdate));
        }

        private static IEnumerable<(string, string)> BuildQueryStringForPaginationRequest(int? limit,
            string cursor,
            SortOrder? sortOrder)
        {
            if (limit.HasValue)
                yield return ("limit", limit.Value.ToString());
            if (!cursor.IsNullOrEmpty())
                yield return ("cursor", cursor);
            if (sortOrder.HasValue)
                yield return ("sortOrder", sortOrder.Value.ToString());
        }

        private static IEnumerable<(string, string)> BuildQueryStringForSearchRequest(string keyword,
            int? limit,
            string cursor)
        {
            yield return ("keyword", keyword);
            foreach (var q in BuildQueryStringForPaginationRequest(limit, cursor, null))
                yield return q;
        }

        private static IEnumerable<(string, string)> BuildQueryStringForDisplayNameValidationRequest(string displayName,
            string birthdate)
        {
            yield return ("displaName", displayName);
            yield return ("birthdate", birthdate);
        }
    }
}