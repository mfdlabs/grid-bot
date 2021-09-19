﻿using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend;

namespace MFDLabs.Hashicorp.VaultClient.V1
{
    /// <summary>
    /// The V1 interface for the Vault Api.
    /// </summary>
    public interface IVaultClientV1
    {
        /// <summary>
        /// The Secrets Engine interface.
        /// </summary>
        ISecretsEngine Secrets { get; }

        /// <summary>
        /// The Auth Method interface.
        /// </summary>
        IAuthMethod Auth { get; }

        /// <summary>
        /// The System Backend interface.
        /// </summary>
        ISystemBackend System { get; }
    }
}
