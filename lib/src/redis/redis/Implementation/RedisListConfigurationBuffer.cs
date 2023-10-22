namespace Redis;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using StackExchange.Redis;

using Logging;

/// <inheritdoc cref="IRedisListConfigurationBuffer"/>
public class RedisListConfigurationBuffer : IRedisListConfigurationBuffer
{
    private const string _CertificateAbsolutePathPropertyName = "RedisBufferCertPath";
    private const string _CertificatePasswordPropertyName = "RedisBufferCertPassword";

    private readonly HashSet<string> _PropertiesRecreateConnection;
    private readonly ILogger _BackupLogger;
    private readonly Func<string> _RedisEndpointGetter;
    private readonly Func<string> _RedisListKeyGetter;
    private readonly Func<string> _RedisPasswordGetter;
    private readonly Func<bool> _RedisAbortConnectOnFailGetter;
    private readonly Func<int> _RedisConnectTimeoutGetter;
    private readonly Func<int> _RedisConnectRetryGetter;
    private readonly Func<string> _CertificateAbsolutePathGetter;
    private readonly Func<string> _CertificatePasswordGetter;

    /// <inheritdoc cref="IRedisListConfigurationBuffer.RedisListKey"/>
    public string RedisListKey => _RedisListKeyGetter();

    /// <summary>
    /// Construct a new instance of <see cref="RedisListConfigurationBuffer"/>
    /// </summary>
    /// <param name="redisEndpointPropertyName">The endpoint property name.</param>
    /// <param name="redisPasswordPropertyName">The password property name.</param>
    /// <param name="redisListKeyPropertyName">The list key property name.</param>
    /// <param name="redisAbortOnConnectFailPropertyName">The abort on connect fail property name.</param>
    /// <param name="redisConnectTimeoutPropertyName">The connect timeout property name.</param>
    /// <param name="redisConnectRetryPropertyName">The connect retry property name.</param>
    /// <param name="backupLogger">The <see cref="ILogger"/></param>
    /// <param name="redisEndpointGetter">The Redis endpoint getter.</param>
    /// <param name="redisListKeyGetter">The Redis list key getter.</param>
    /// <param name="redisAbortConnectOnFailGetter"></param>
    /// <param name="redisConnectTimeoutGetter"></param>
    /// <param name="redisConnectRetryGetter"></param>
    /// <param name="certificateAbsolutePathGetter"></param>
    /// <param name="certificatePasswordGetter"></param>
    /// <param name="redisPasswordGetter"></param>
    public RedisListConfigurationBuffer(
        string redisEndpointPropertyName, 
        string redisPasswordPropertyName, 
        string redisListKeyPropertyName, 
        string redisAbortOnConnectFailPropertyName, 
        string redisConnectTimeoutPropertyName,
        string redisConnectRetryPropertyName, 
        ILogger backupLogger, 
        Func<string> redisEndpointGetter,
        Func<string> redisListKeyGetter, 
        Func<bool> redisAbortConnectOnFailGetter, 
        Func<int> redisConnectTimeoutGetter, 
        Func<int> redisConnectRetryGetter,
        Func<string> certificateAbsolutePathGetter, 
        Func<string> certificatePasswordGetter, 
        Func<string> redisPasswordGetter = null
    )
    {
        _PropertiesRecreateConnection = new HashSet<string>
        {
            redisEndpointPropertyName,
            redisPasswordPropertyName,
            redisListKeyPropertyName,
            redisAbortOnConnectFailPropertyName,
            redisConnectTimeoutPropertyName,
            redisConnectRetryPropertyName,
            _CertificateAbsolutePathPropertyName,
            _CertificatePasswordPropertyName
        };

        _BackupLogger = backupLogger;
        _RedisEndpointGetter = redisEndpointGetter;
        _RedisListKeyGetter = redisListKeyGetter;
        _RedisPasswordGetter = redisPasswordGetter;
        _RedisAbortConnectOnFailGetter = redisAbortConnectOnFailGetter;
        _RedisConnectTimeoutGetter = redisConnectTimeoutGetter;
        _RedisConnectRetryGetter = redisConnectRetryGetter;
        _CertificateAbsolutePathGetter = certificateAbsolutePathGetter;
        _CertificatePasswordGetter = certificatePasswordGetter;
    }

    /// <inheritdoc cref="IRedisListConfigurationBuffer.OptionsOnCertificateSelection(object, string, X509CertificateCollection, X509Certificate, string[])"/>
    public X509Certificate OptionsOnCertificateSelection(
        object sender, 
        string targetHost, 
        X509CertificateCollection localCertificates, 
        X509Certificate remoteCertificate, 
        string[] acceptableIssuers
    )
    {
        try
        {
            return new X509Certificate2(_CertificateAbsolutePathGetter(), _CertificatePasswordGetter());
        }
        catch (CryptographicException ex)
        {
            _BackupLogger.Error("There was a CryptographicException while trying to read a certificate from {0}: {1}", _CertificateAbsolutePathGetter(), ex);
            throw;
        }
        catch (Exception ex)
        {
            _BackupLogger.Error("There was an general exception thrown while trying to read certificates: {0}", ex);
            throw;
        }
    }

    /// <inheritdoc cref="IRedisListConfigurationBuffer.GetConfigurationOptions"/>
    public ConfigurationOptions GetConfigurationOptions()
    {
        var options = new ConfigurationOptions();

        options.EndPoints.Add(_RedisEndpointGetter());
        options.Ssl = true;
        options.AbortOnConnectFail = _RedisAbortConnectOnFailGetter();
        options.ConnectTimeout = _RedisConnectTimeoutGetter();
        options.ConnectRetry = _RedisConnectRetryGetter();

        if (!string.IsNullOrEmpty(_RedisPasswordGetter?.Invoke()))
            options.Password = _RedisPasswordGetter();

        options.CertificateSelection += OptionsOnCertificateSelection;

        return options;
    }

    /// <inheritdoc cref="IRedisListConfigurationBuffer.NeedsReCreation(string)"/>
    public bool NeedsReCreation(string propertyName) => _PropertiesRecreateConnection.Contains(propertyName);
}
