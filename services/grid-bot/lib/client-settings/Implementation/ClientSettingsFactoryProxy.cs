using ClientSettings.Client;

namespace Grid.Bot.ClientSettings;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Proxy <see cref="IClientSettingsClient"/> for GSPM.
/// </summary>
/// <remarks>
/// The only method that is implemented is <see cref="IClientSettingsClient.GetRccOnlyClientApplicationSettings(string, string)" />
/// </remarks>
public class ClientSettingsFactoryProxyClient : IClientSettingsClient
{
    private readonly IClientSettingsFactory _clientSettingsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientSettingsFactoryProxyClient"/> class.
    /// </summary>
    /// <param name="clientSettingsFactory">The <see cref="IClientSettingsFactory"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="clientSettingsFactory"/> is null.</exception>
    public ClientSettingsFactoryProxyClient(IClientSettingsFactory clientSettingsFactory)
    {
        _clientSettingsFactory = clientSettingsFactory ?? throw new ArgumentNullException(nameof(clientSettingsFactory));
    }

    /// <inheritdoc cref="IClientSettingsClient.GetRccOnlyClientApplicationSettings(string, string)"/>
    public ClientApplicationSettingsResponse GetRccOnlyClientApplicationSettings(string applicationName, string bucketName)
      => new()
      {
          ApplicationSettings = !string.IsNullOrWhiteSpace(bucketName)
            ? _clientSettingsFactory.GetBucketedSettingsForApplication(applicationName, bucketName)
            : _clientSettingsFactory.GetSettingsForApplication(applicationName)
      };

    /// <inheritdoc cref="IClientSettingsClient.GetRccOnlyClientApplicationSettingsAsync(string, string)"/>
    public Task<ClientApplicationSettingsResponse> GetRccOnlyClientApplicationSettingsAsync(string applicationName, string bucketName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetRccOnlyClientApplicationSettingsAsync(string, string, CancellationToken)"/>
    public Task<ClientApplicationSettingsResponse> GetRccOnlyClientApplicationSettingsAsync(string applicationName, string bucketName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}