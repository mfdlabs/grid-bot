namespace Grid.Bot.Utility;

using System;
using System.Threading;
using System.Threading.Tasks;

using ClientSettings.Client;


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

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public ClientApplicationSettingsResponse GetApplicationSettings(string applicationName, string x_Api_Key = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<ClientApplicationSettingsResponse> GetApplicationSettingsAsync(string applicationName, string x_Api_Key = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<ClientApplicationSettingsResponse> GetApplicationSettingsAsync(string applicationName, string x_Api_Key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public ClientApplicationSettingResponse GetClientApplicationSetting(string applicationName, string settingName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<ClientApplicationSettingResponse> GetClientApplicationSettingAsync(string applicationName, string settingName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<ClientApplicationSettingResponse> GetClientApplicationSettingAsync(string applicationName, string settingName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<ClientApplicationSettingsResponse> GetRccOnlyClientApplicationSettingsAsync(string applicationName, string bucketName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<ClientApplicationSettingsResponse> GetRccOnlyClientApplicationSettingsAsync(string applicationName, string bucketName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public void ImportApplicationSetting(string x_Api_Key, ImportClientApplicationSettingsRequest request)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task ImportApplicationSettingAsync(string x_Api_Key, ImportClientApplicationSettingsRequest request)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task ImportApplicationSettingAsync(string x_Api_Key, ImportClientApplicationSettingsRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public void RefreshAllClientApplicationSettings(string x_Api_Key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task RefreshAllClientApplicationSettingsAsync(string x_Api_Key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task RefreshAllClientApplicationSettingsAsync(string x_Api_Key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public SetClientApplicationSettingResponse SetClientApplicationSetting(string x_Api_Key, SetClientApplicationSettingRequest request)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<SetClientApplicationSettingResponse> SetClientApplicationSettingAsync(string x_Api_Key, SetClientApplicationSettingRequest request)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc cref="IClientSettingsClient.GetApplicationSettings"/>
    public Task<SetClientApplicationSettingResponse> SetClientApplicationSettingAsync(string x_Api_Key, SetClientApplicationSettingRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}