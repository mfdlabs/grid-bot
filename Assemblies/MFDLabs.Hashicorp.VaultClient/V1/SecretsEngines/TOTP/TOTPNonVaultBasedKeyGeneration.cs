﻿namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.TOTP
{
    /// <summary>
    /// Specifies that the key is generated by some other service.
    /// </summary>
    public class TOTPNonVaultBasedKeyGeneration : AbstractTOTPKeyGenerationOption
    {
        /// <summary>
        /// Specifies the TOTP key url string that can be used to configure a key. 
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Specifies the master key used to generate a TOTP code.
        /// </summary>
        public string Key { get; set; }
    }
}