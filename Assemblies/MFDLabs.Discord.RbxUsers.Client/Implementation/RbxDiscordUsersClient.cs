using System;
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

            return _RequestSender.SendRequest<RobloxUserResponse>(HttpMethod.Get, $"/api/user/{discordID}");
        }
        public Task<RobloxUserResponse> ResolveRobloxUserByIDAsync(ulong discordID, CancellationToken cancellationToken)
        {
            if (discordID == default) throw new ArgumentNullException("discordID");

            return _RequestSender.SendRequestAsync<RobloxUserResponse>(HttpMethod.Get, $"/api/user/{discordID}", cancellationToken);
        }
    }
}
