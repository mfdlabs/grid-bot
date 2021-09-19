using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.UserPass
{
    internal class UserPassAuthMethodLoginProvider : IAuthMethodLoginProvider
    {
        private readonly UserPassAuthMethodInfo _userPassAuthMethodInfo;
        private readonly Polymath _polymath;

        public UserPassAuthMethodLoginProvider(UserPassAuthMethodInfo userPassAuthenticationInfo, Polymath polymath)
        {
            _userPassAuthMethodInfo = userPassAuthenticationInfo;
            _polymath = polymath;
        }

        public async Task<string> GetVaultTokenAsync()
        {
            var requestData = new Dictionary<string, object>
            {
                {"password", _userPassAuthMethodInfo.Password }
            };

            // make an unauthenticated call to Vault, since this is the call to get the token. It shouldn't need a token.
            var response = await _polymath.MakeVaultApiRequest<Secret<Dictionary<string, object>>>(LoginResourcePath, HttpMethod.Post, requestData, unauthenticated: true).ConfigureAwait(_polymath.VaultClientSettings.ContinueAsyncTasksOnCapturedContext);
            _userPassAuthMethodInfo.ReturnedLoginAuthInfo = response?.AuthInfo;

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
                var endpoint = string.Format(CultureInfo.InvariantCulture, "v1/auth/{0}/login/{1}", _userPassAuthMethodInfo.MountPoint.Trim('/'), _userPassAuthMethodInfo.Username);
                return endpoint;
            }
        }
    }
}