using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines
{
    /// <summary>
    /// Represents credentials with a username and password.
    /// </summary>
    public class UsernamePasswordCredentials
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        [JsonProperty("password")]
        public string Password { get; set; }
    }
}