﻿using MFDLabs.Hashicorp.VaultClient.V1;

namespace MFDLabs.Hashicorp.VaultClient
{
    /// <summary>
    /// Provides an interface to interact with Vault as a client.
    /// This is the only entry point for consuming the Vault Client.
    /// </summary>
    public interface IVaultClient
    {
        /// <summary>
        /// Gets the Vault Client Settings.
        /// </summary>
        VaultClientSettings Settings { get; }

        /// <summary>
        /// Gets the V1 Client interface for Vault Api.
        /// </summary>
        IVaultClientV1 V1 { get; }
    }
}

