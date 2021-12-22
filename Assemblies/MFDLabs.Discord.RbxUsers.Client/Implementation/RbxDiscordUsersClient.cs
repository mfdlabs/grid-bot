using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Discord.RbxUsers.Client.Models;
using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Instrumentation;

namespace MFDLabs.Discord.RbxUsers.Client
{
    public class RbxDiscordUsersClient : IRbxDiscordUsersClient
    {
        public RbxDiscordUsersClient(ICounterRegistry counterRegistry, RbxDiscordUsersClientConfig config)
        {
            var httpClientSettings = new RbxDiscordUsersClientSettings(config);
            _requestSender = new HttpRequestSender(new RbxDiscordUsersHttpClientBuilder(counterRegistry,
                    httpClientSettings,
                    config).Build(),
                new HttpRequestBuilder(httpClientSettings.Endpoint));
        }
        
        private readonly IHttpRequestSender _requestSender;

        public RobloxUserResponse ResolveRobloxUserById(ulong discordId)
        {
            if (discordId == default) throw new ArgumentNullException(nameof(discordId));
            if (_cachedUsers.TryGetValue(discordId, out var userResponse)) return userResponse;
            userResponse = _requestSender.SendRequest<RobloxUserResponse>(HttpMethod.Get, $"/api/user/{discordId}");
            _cachedUsers.TryAdd(discordId, userResponse);
            return userResponse;
        }
        public async Task<RobloxUserResponse> ResolveRobloxUserByIdAsync(ulong discordId, CancellationToken cancellationToken)
        {
            if (discordId == default) throw new ArgumentNullException(nameof(discordId));
            if (_cachedUsers.TryGetValue(discordId, out var userResponse)) return userResponse;
            userResponse = await _requestSender.SendRequestAsync<RobloxUserResponse>(HttpMethod.Get, $"/api/user/{discordId}", cancellationToken);
            _cachedUsers.TryAdd(discordId, userResponse);
            return userResponse;
        }

        private readonly ConcurrentDictionary<ulong, RobloxUserResponse> _cachedUsers = new();
    }
}
