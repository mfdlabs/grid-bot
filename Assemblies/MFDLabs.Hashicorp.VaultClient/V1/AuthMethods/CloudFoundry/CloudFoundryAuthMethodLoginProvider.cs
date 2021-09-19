﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.CloudFoundry
{
    internal class CloudFoundryAuthMethodLoginProvider : IAuthMethodLoginProvider
    {
        private readonly CloudFoundryAuthMethodInfo _cloudFoundryAuthMethodInfo;
        private readonly Polymath _polymath;        

        public CloudFoundryAuthMethodLoginProvider(CloudFoundryAuthMethodInfo cloudFoundryAuthMethodInfo, Polymath polymath)
        {
            _cloudFoundryAuthMethodInfo = cloudFoundryAuthMethodInfo;
            _polymath = polymath;
        }

        public static string GetFormattedSigningTime(DateTime signingTime)
        {
            return signingTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public async Task<string> GetVaultTokenAsync()
        {
            var requestData = new
            {
                role = _cloudFoundryAuthMethodInfo.RoleName,
                cf_instance_cert = _cloudFoundryAuthMethodInfo.CFInstanceCertContent,
                signing_time = GetFormattedSigningTime(_cloudFoundryAuthMethodInfo.SignatureDateTime),
                signature = _cloudFoundryAuthMethodInfo.Signature
            };
            
            // make an unauthenticated call to Vault, since this is the call to get the token. 
            // It shouldn't need a token.
            var response = await _polymath.MakeVaultApiRequest<Secret<Dictionary<string, object>>>(LoginResourcePath, HttpMethod.Post, requestData, unauthenticated: true).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
            
            _cloudFoundryAuthMethodInfo.ReturnedLoginAuthInfo = response?.AuthInfo;

            if (response?.AuthInfo != null && !string.IsNullOrWhiteSpace(response.AuthInfo.ClientToken))
            {
                return response.AuthInfo.ClientToken;
            }

            throw new Exception("The call to the Vault authentication method backend did not yield a client token. Please verify your credentials.");
        }

        private string LoginResourcePath
        {
            get
            {
                var endpoint = string.Format(CultureInfo.InvariantCulture, "v1/auth/{0}/login", _cloudFoundryAuthMethodInfo.MountPoint.Trim('/'));
                return endpoint;
            }
        }
    }
}
