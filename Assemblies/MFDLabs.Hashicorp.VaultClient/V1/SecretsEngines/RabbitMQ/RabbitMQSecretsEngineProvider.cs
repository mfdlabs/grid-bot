﻿using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.RabbitMQ
{
    internal class RabbitMQSecretsEngineProvider : IRabbitMQSecretsEngine
    {
        private readonly Polymath _polymath;

        public RabbitMQSecretsEngineProvider(Polymath polymath)
        {
            _polymath = polymath;
        }

        public async Task<Secret<UsernamePasswordCredentials>> GetCredentialsAsync(string roleName, string mountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(roleName, "roleName");

            return await _polymath.MakeVaultApiRequest<Secret<UsernamePasswordCredentials>>(mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.RabbitMQ, "/creds/" + roleName.Trim('/'), HttpMethod.Get, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }
    }
}