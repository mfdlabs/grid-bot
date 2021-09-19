﻿namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.TOTP
{
    public class TOTPCreateKeyRequest
    {
        /// <summary>
        /// Gets or sets if a key should be generated by Vault or if a key is being passed from another service.
        /// </summary>
        public AbstractTOTPKeyGenerationOption KeyGenerationOption { get; set; }

        /// <summary>
        /// Gets or sets the name of the issuing organization.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the name of the account associated with the key.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Specifies the length of time in seconds used to generate a counter for the TOTP code calculation.
        /// </summary>
        public string Period { get; set; }

        /// <summary>
        /// Specifies the hashing algorithm used to generate the TOTP code. 
        /// Options include "SHA1", "SHA256" and "SHA512".
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the number of digits in the generated TOTP code.
        /// This value can be set to 6 or 8.
        /// </summary>
        public int Digits { get; set; }

        public TOTPCreateKeyRequest()
        {
            Period = "30";
            Algorithm = "SHA1";
            Digits = 6;
        }
    }
}