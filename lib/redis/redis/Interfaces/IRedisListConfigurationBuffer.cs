namespace Redis;

using System.Security.Cryptography.X509Certificates;

using StackExchange.Redis;

/// <summary>
/// Redis list configuration buffer
/// </summary>
public interface IRedisListConfigurationBuffer
{
    /// <summary>
    /// Key for Redis lists
    /// </summary>
    string RedisListKey { get; }

    /// <summary>
    /// Get the configuration options.
    /// </summary>
    /// <returns>The configuration options.</returns>
    ConfigurationOptions GetConfigurationOptions();

    /// <summary>
    /// Does this <see cref="IRedisListConfigurationBuffer"/> need to be recreated?
    /// </summary>
    /// <param name="propertyName">The property</param>
    /// <returns>True if needs recreation</returns>
    bool NeedsReCreation(string propertyName);

    /// <summary>
    /// Options on certificate selection
    /// </summary>
    /// <param name="sender">The sender</param>
    /// <param name="targetHost">The target host</param>
    /// <param name="localCertificates">The <see cref="X509CertificateCollection"/></param>
    /// <param name="remoteCertificate">The <see cref="X509Certificate"/></param>
    /// <param name="acceptableIssuers">The acceptable issuers.</param>
    /// <returns>The certificate.</returns>
    X509Certificate OptionsOnCertificateSelection(
        object sender,
        string targetHost, 
        X509CertificateCollection localCertificates, 
        X509Certificate remoteCertificate,
        string[] acceptableIssuers
    );
}
