﻿using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Transit
{
    internal class TransitSecretsEngineProvider : ITransitSecretsEngine
    {
        private readonly Polymath _polymath;

        public TransitSecretsEngineProvider(Polymath polymath)
        {
            _polymath = polymath;
        }

        public async Task<Secret<EncryptionResponse>> EncryptAsync(string keyName, EncryptRequestOptions encryptRequestOptions, string mountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(keyName, "keyName");
            return await _polymath.MakeVaultApiRequest<Secret<EncryptionResponse>>(mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/encrypt/" + keyName.Trim('/'), HttpMethod.Post, encryptRequestOptions, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<DecryptionResponse>> DecryptAsync(string keyName, DecryptRequestOptions decryptRequestOptions, string mountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(keyName, "keyName");
            return await _polymath.MakeVaultApiRequest<Secret<DecryptionResponse>>(mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/decrypt/" + keyName.Trim('/'), HttpMethod.Post, decryptRequestOptions, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<ListInfo>> ReadAllEncryptionKeysAsync(string mountPoint = null, string wrapTimeToLive = null)
        {

            return await _polymath.MakeVaultApiRequest<Secret<ListInfo>>(mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys?list=true", HttpMethod.Get, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<DataKeyResponse>> GenerateDataKeyAsync(string keyType, string keyName, DataKeyRequestOptions dataKeyRequestOptions, string mountPoint = null, string wrapTimeToLive = null)
        {
            Checker.NotNull(keyType, "keyType");
            Checker.NotNull(keyName, "keyName");

            return await _polymath.MakeVaultApiRequest<Secret<DataKeyResponse>>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/datakey/" + keyType.Trim('/')+ "/" + keyName.Trim('/'), HttpMethod.Post, dataKeyRequestOptions, wrapTimeToLive: wrapTimeToLive).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task CreateEncryptionKeyAsync(string keyName, CreateKeyRequestOptions createKeyRequestOptions, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");
            Checker.NotNull(createKeyRequestOptions, "createKeyRequestOptions");

            await _polymath.MakeVaultApiRequest<object>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys/" + keyName.Trim('/'),
                HttpMethod.Post,
                createKeyRequestOptions)
                    .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<EncryptionKeyInfo>> ReadEncryptionKeyAsync(string keyName, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");

            return await _polymath.MakeVaultApiRequest<Secret<EncryptionKeyInfo>>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys/" + keyName.Trim('/'),
                HttpMethod.Get)
                    .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task UpdateEncryptionKeyConfigAsync(string keyName, UpdateKeyRequestOptions updateKeyRequestOptions, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");
            Checker.NotNull(updateKeyRequestOptions, "updateKeyRequestOptions");

            await _polymath.MakeVaultApiRequest<object>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys/" + keyName.Trim('/') + "/config",
                HttpMethod.Post,
                updateKeyRequestOptions)
                    .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task DeleteEncryptionKeyAsync(string keyName, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");

            await _polymath.MakeVaultApiRequest<object>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys/" + keyName.Trim('/'),
                HttpMethod.Delete)
                    .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task RotateEncryptionKeyAsync(string keyName, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");

            await _polymath.MakeVaultApiRequest<object>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys/" + keyName.Trim('/') + "/rotate",
                HttpMethod.Post)
                    .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<EncryptionResponse>> RewrapAsync(string keyName, RewrapRequestOptions rewrapRequestOptions, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");
            Checker.NotNull(rewrapRequestOptions, "rewrapRequestOptions");

            return await _polymath.MakeVaultApiRequest<Secret<EncryptionResponse>>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/rewrap/" + keyName.Trim('/'),
                HttpMethod.Post,
                rewrapRequestOptions)
                    .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task TrimKeyAsync(string keyName, TrimKeyRequestOptions trimKeyRequestOptions, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");
            Checker.NotNull(trimKeyRequestOptions, "trimKeyRequestOptions");

            await _polymath.MakeVaultApiRequest<object>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit, "/keys/" + keyName.Trim('/') + "/trim",
                HttpMethod.Post,
                trimKeyRequestOptions)
                .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
        }

        public async Task<Secret<ExportedKeyInfo>> ExportKeyAsync(TransitKeyCategory keyType, 
            string keyName, string version = null, string mountPoint = null)
        {
            Checker.NotNull(keyName, "keyName");

            return await _polymath.MakeVaultApiRequest<Secret<ExportedKeyInfo>>(
                mountPoint ?? _polymath.VaultClientSettings.SecretsEngineMountPoints.Transit,
                "/export/" + keyType.ToString().Replace("_", "-") + "/" + keyName + 
                (string.IsNullOrEmpty(version) ? "" : "/" + version),
                HttpMethod.Get)
                .ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);

        }
    }
}
