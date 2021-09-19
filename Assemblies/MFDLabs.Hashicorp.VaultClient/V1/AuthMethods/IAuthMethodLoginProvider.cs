﻿using System.Threading.Tasks;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods
{
    /// <summary>
    /// Auth Method login provider.
    /// </summary>
    internal interface IAuthMethodLoginProvider
    {
        /// <summary>
        /// The login method for the auth method.
        /// </summary>
        /// <returns>The Vault Token.</returns>
        Task<string> GetVaultTokenAsync();
    }
}
