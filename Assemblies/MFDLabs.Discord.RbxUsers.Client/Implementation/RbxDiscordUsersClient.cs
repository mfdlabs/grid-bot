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
            var CountersHttpClientSettings = new RbxDiscordUsersClientSettings(config);
            var httpClientBuilder = new RbxDiscordUsersHttpClientBuilder(counterRegistry, CountersHttpClientSettings, config);
            var httpRequestBuilder = new HttpRequestBuilder(CountersHttpClientSettings.Endpoint);
            var httpClient = httpClientBuilder.Build();
            _RequestSender = new HttpRequestSender(httpClient, httpRequestBuilder);
        }
        
        private readonly IHttpRequestSender _RequestSender;

        public RobloxUserResponse ResolveRobloxUserByID(ulong discordID)
        {
            if (discordID == default) throw new ArgumentNullException("discordID");
            if (_cachedUsers.TryGetValue(discordID, out var userResponse)) return userResponse;
            userResponse = _RequestSender.SendRequest<RobloxUserResponse>(HttpMethod.Get, $"/api/user/{discordID}");
            _cachedUsers.TryAdd(discordID, userResponse);
            return userResponse;
        }
        public async Task<RobloxUserResponse> ResolveRobloxUserByIDAsync(ulong discordID, CancellationToken cancellationToken)
        {
            if (discordID == default) throw new ArgumentNullException("discordID");
            if (_cachedUsers.TryGetValue(discordID, out var userResponse)) return userResponse;
            userResponse = await _RequestSender.SendRequestAsync<RobloxUserResponse>(HttpMethod.Get, $"/api/user/{discordID}", cancellationToken);
            _cachedUsers.TryAdd(discordID, userResponse);
            return userResponse;
        }

        private readonly ConcurrentDictionary<ulong, RobloxUserResponse> _cachedUsers = new ConcurrentDictionary<ulong, RobloxUserResponse>();
    }
}
