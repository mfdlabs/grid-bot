﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Kerberos
{
    internal class KerberosAuthMethodLoginProvider : IAuthMethodLoginProvider
    {
        private readonly KerberosAuthMethodInfo _kerberosAuthMethodInfo;
        private readonly Polymath _polymath;

        public KerberosAuthMethodLoginProvider(KerberosAuthMethodInfo kerberosAuthMethodInfo, Polymath polymath)
        {
            _kerberosAuthMethodInfo = kerberosAuthMethodInfo;
            _polymath = polymath;
        }

        public async Task<string> GetVaultTokenAsync()
        {
            // make an unauthenticated call to Vault, since this is the call to get the token. It shouldn't need a token.
            var response = await _polymath.MakeVaultApiRequest<Secret<Dictionary<string, object>>>(LoginResourcePath, HttpMethod.Post, unauthenticated: true).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
            _kerberosAuthMethodInfo.ReturnedLoginAuthInfo = response?.AuthInfo;

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
                var endpoint = string.Format(CultureInfo.InvariantCulture, "v1/auth/{0}/login", _kerberosAuthMethodInfo.MountPoint.Trim('/'));
                return endpoint;
            }
        }
    }
}
