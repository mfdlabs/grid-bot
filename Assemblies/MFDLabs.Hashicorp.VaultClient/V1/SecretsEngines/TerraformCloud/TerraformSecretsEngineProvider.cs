﻿using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Terraform
{
    internal class TerraformSecretsEngineProvider : ITerraformSecretsEngine
    {
        private readonly Polymath _polymath;

        public TerraformSecretsEngineProvider(Polymath polymath)
        {
            _polymath = polymath;
        }

        public async Task<Secret<TerraformCredentials>> GetCredentialsAsync(string roleName, string mountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(roleName, "roleName");

            return await _polymath.MakeVaultApiRequest<Secret<TerraformCredentials>>(mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Terraform, "/creds/" + roleName.Trim('/'), HttpMethod.Get, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }
    }
}