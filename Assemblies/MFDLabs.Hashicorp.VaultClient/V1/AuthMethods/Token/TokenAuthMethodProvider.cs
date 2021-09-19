﻿using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Token.Models;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Token
{
    internal class TokenAuthMethodProvider : ITokenAuthMethod
    {
        private readonly Polymath _polymath;

        public TokenAuthMethodProvider(Polymath polymath)
        {
            Checker.NotNull(polymath, "polymath");
            this._polymath = polymath;
        }

        public async Task<Secret<object>> CreateTokenAsync(CreateTokenRequest createTokenRequest)
        {
            var request = createTokenRequest ?? new CreateTokenRequest();

            var suffix = "create";

            if (request.CreateOrphan)
            {
                suffix = "create-orphan";
            }

            if (!string.IsNullOrWhiteSpace(request.RoleName))
            {
                suffix = suffix + "/" + request.RoleName.Trim('/');
            }

            return await _polymath.MakeVaultApiRequest<Secret<object>>("v1/auth/token/" + suffix, HttpMethod.Post, request).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<ClientTokenInfo>> LookupAsync(string clientToken)
        {
            Checker.NotNull(clientToken, nameof(clientToken));

            var requestData = new { token = clientToken };
            return await _polymath.MakeVaultApiRequest<Secret<ClientTokenInfo>>("v1/auth/token/lookup", HttpMethod.Post, requestData).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<CallingTokenInfo>> LookupSelfAsync()
        {
            return await _polymath.MakeVaultApiRequest<Secret<CallingTokenInfo>>("v1/auth/token/lookup-self", HttpMethod.Get).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<AuthInfo> RenewSelfAsync(string increment = null)
        {
            var requestData = !string.IsNullOrWhiteSpace(increment) ? new { increment = increment } : null;

            var result = await _polymath.MakeVaultApiRequest<Secret<JToken>>("v1/auth/token/renew-self", HttpMethod.Post, requestData).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
            return result.AuthInfo;
        }

        public async Task RevokeSelfAsync()
        {
            await _polymath.MakeVaultApiRequest<Secret<JToken>>("v1/auth/token/revoke-self", HttpMethod.Post).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }
    }
}