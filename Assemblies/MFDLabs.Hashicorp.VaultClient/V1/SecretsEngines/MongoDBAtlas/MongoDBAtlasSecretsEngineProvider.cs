﻿using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.MongoDBAtlas
{
    internal class MongoDBAtlasSecretsEngineProvider : IMongoDBAtlasSecretsEngine
    {
        private readonly Polymath _polymath;

        public MongoDBAtlasSecretsEngineProvider(Polymath polymath)
        {
            _polymath = polymath;
        }

        public async Task<Secret<MongoDBAtlasCredentials>> GetCredentialsAsync(string name, string mountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(name, "name");

            return await _polymath.MakeVaultApiRequest<Secret<MongoDBAtlasCredentials>>(mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.MongoDBAtlas, "/creds/" + name.Trim('/'), HttpMethod.Get, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }
    }
}