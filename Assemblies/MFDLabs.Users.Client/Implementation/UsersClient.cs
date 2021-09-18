using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;
using MFDLabs.Users.Client.Enumeration;
using MFDLabs.Users.Client.Models.Usernames;
using MFDLabs.Users.Client.Models.Users;
using MFDLabs.Users.Client.Models.UserSearch;
using MFDLabs.Users.Client.Models.WebAPI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Users.Client
{
    public class UsersClient : IUsersClient
    {
        public UsersClient(ICounterRegistry counterRegistry, UsersClientConfig config)
        {
            var CountersHttpClientSettings = new UsersClientSettings(config);
            var httpClientBuilder = new UsersHttpClientBuilder(counterRegistry, CountersHttpClientSettings, config);
            var httpRequestBuilder = new HttpRequestBuilder(CountersHttpClientSettings.Endpoint);
            var httpClient = httpClientBuilder.Build();
            _RequestSender = new HttpRequestSender(httpClient, httpRequestBuilder);
        }

        private IEnumerable<(string, string)> BuildQueryStringForPaginationRequest(int? limit, string cursor, SortOrder? sortOrder)
        {
            if (limit.HasValue)
                yield return ("limit", limit.Value.ToString());
            if (!cursor.IsNullOrEmpty())
                yield return ("cursor", cursor);
            if (sortOrder.HasValue)
                yield return ("sortOrder", sortOrder.Value.ToString());
            yield break;
        }

        private IEnumerable<(string, string)> BuildQueryStringForSearchRequest(string keyword, int? limit, string cursor)
        {
            yield return ("keyword", keyword);
            foreach (var q in BuildQueryStringForPaginationRequest(limit, cursor, null))
            {
                yield return q;
            }

            yield break;
        }

        private IEnumerable<(string, string)> BuildQueryStringForDisplayNameValidationRequest(string displayName, string birthdate)
        {
            yield return ("displaName", displayName);
            yield return ("birthdate", birthdate);
            yield break;
        }

        public UserResponseV2 GetUserDetails(long userId)
        {
            return _RequestSender.SendRequest<UserResponseV2>(HttpMethod.Get, $"/v1/users/{userId}");
        }

        public Task<UserResponseV2> GetUserDetailsAsync(long userId)
        {
            return _RequestSender.SendRequestAsync<UserResponseV2>(HttpMethod.Get, $"/v1/users/{userId}", CancellationToken.None);
        }

        public ApiPageResponse<UsernameHistoryResponse> GetUsernameHistory(long userId, int? limit, string cursor, SortOrder? sortOrder)
        {
            return _RequestSender.SendRequest<ApiPageResponse<UsernameHistoryResponse>>(HttpMethod.Get, $"/v1/users/{userId}/username-history", BuildQueryStringForPaginationRequest(limit, cursor, sortOrder));
        }

        public Task<ApiPageResponse<UsernameHistoryResponse>> GetUsernameHistoryAsync(long userId, int? limit, string cursor, SortOrder? sortOrder)
        {
            return _RequestSender.SendRequestAsync<ApiPageResponse<UsernameHistoryResponse>>(HttpMethod.Get, $"/v1/users/{userId}/username-history", CancellationToken.None, BuildQueryStringForPaginationRequest(limit, cursor, sortOrder));
        }

        public UserStatusResponse GetUserStatus(long userId)
        {
            return _RequestSender.SendRequest<UserStatusResponse>(HttpMethod.Get, $"/v1/users/{userId}/status");
        }

        public Task<UserStatusResponse> GetUserStatusAsync(long userId)
        {
            return _RequestSender.SendRequestAsync<UserStatusResponse>(HttpMethod.Get, $"/v1/users/{userId}/status", CancellationToken.None);
        }

        public ApiArrayResponse<SkinnyUserResponse> MultiGetUsersByIds(MultiGetByUserIdRequest request)
        {
            return _RequestSender.SendRequestWithJsonBody<MultiGetByUserIdRequest, ApiArrayResponse<SkinnyUserResponse>>(HttpMethod.Post, $"/v1/users", request, null);
        }

        public Task<ApiArrayResponse<SkinnyUserResponse>> MultiGetUsersByIdsAsync(MultiGetByUserIdRequest request)
        {
            return _RequestSender.SendRequestWithJsonBodyAsync<MultiGetByUserIdRequest, ApiArrayResponse<SkinnyUserResponse>>(HttpMethod.Post, $"/v1/users", request, CancellationToken.None, null);
        }

        public ApiArrayResponse<MultiGetUserByNameResponse> MultiGetUsersByUsernames(MultiGetByUsernameRequest request)
        {
            return _RequestSender.SendRequestWithJsonBody<MultiGetByUsernameRequest, ApiArrayResponse<MultiGetUserByNameResponse>>(HttpMethod.Post, $"/v1/usernames/users", request, null);
        }

        public Task<ApiArrayResponse<MultiGetUserByNameResponse>> MultiGetUsersByUsernamesAsync(MultiGetByUsernameRequest request)
        {
            return _RequestSender.SendRequestWithJsonBodyAsync<MultiGetByUsernameRequest, ApiArrayResponse<MultiGetUserByNameResponse>>(HttpMethod.Post, $"/v1/usernames/users", request, CancellationToken.None, null);
        }

        public ApiPageResponse<UserSearchUserResponse> SearchUsers(string keyword, int? limit, string cursor)
        {
            if (keyword.IsNullOrEmpty()) throw new ArgumentNullException("keyword");

            return _RequestSender.SendRequest<ApiPageResponse<UserSearchUserResponse>>(HttpMethod.Get, $"/v1/users/search", BuildQueryStringForSearchRequest(keyword, limit, cursor));
        }

        public Task<ApiPageResponse<UserSearchUserResponse>> SearchUsersAsync(string keyword, int? limit, string cursor)
        {
            if (keyword.IsNullOrEmpty()) throw new ArgumentNullException("keyword");

            return _RequestSender.SendRequestAsync<ApiPageResponse<UserSearchUserResponse>>(HttpMethod.Get, $"/v1/users/search", CancellationToken.None, BuildQueryStringForSearchRequest(keyword, limit, cursor));
        }

        public ApiEmptyResponseModel ValidateNewUserDisplayName(string displayName, string birthdate)
        {
            if (!DateTime.TryParse(birthdate, out _)) throw new ApplicationException("Invalid birthdate for display name validation request.");

            return _RequestSender.SendRequest<ApiEmptyResponseModel>(HttpMethod.Get, $"/v1/display-names/validate", BuildQueryStringForDisplayNameValidationRequest(displayName, birthdate));
        }

        public Task<ApiEmptyResponseModel> ValidateNewUserDisplayNameAsync(string displayName, string birthdate)
        {
            if (!DateTime.TryParse(birthdate, out _)) throw new ApplicationException("Invalid birthdate for display name validation request.");

            return _RequestSender.SendRequestAsync<ApiEmptyResponseModel>(HttpMethod.Get, $"/v1/display-names/validate", CancellationToken.None, BuildQueryStringForDisplayNameValidationRequest(displayName, birthdate));
        }

        private readonly IHttpRequestSender _RequestSender;
    }
}
