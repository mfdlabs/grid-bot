﻿using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Azure
{
    internal class AzureSecretsEngineProvider : IAzureSecretsEngine
    {
        private readonly Polymath _polymath;

        public AzureSecretsEngineProvider(Polymath polymath)
        {
            _polymath = polymath;
        }

        public async Task<Secret<AzureCredentials>> GetCredentialsAsync(string azureRoleName, string azureBackendMountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(azureRoleName, "azureRoleName");

            return await _polymath.MakeVaultApiRequest<Secret<AzureCredentials>>(azureBackendMountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Azure, "/creds/" + azureRoleName.Trim('/'), HttpMethod.Get, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }
    }
}