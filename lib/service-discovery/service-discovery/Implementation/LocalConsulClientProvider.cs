namespace ServiceDiscovery;

using System;
using System.Net;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using Consul;

using ISettings = global::ServiceDiscovery.Properties.ISettings;

/// <inheritdoc cref="IConsulClientProvider"/>
public class LocalConsulClientProvider : IConsulClientProvider, INotifyPropertyChanged, IDisposable
{
    private readonly ISettings _Settings;

    /// <inheritdoc cref="IConsulClientProvider.Client"/>
    public IConsulClient Client { get; private set; }

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Construct a new instance of <see cref="LocalConsulClientProvider"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public LocalConsulClientProvider()
        : this(global::ServiceDiscovery.Properties.Settings.Default)
    {}

    /// <summary>
    /// Construct a new instance of <see cref="LocalConsulClientProvider"/>
    /// </summary>
    /// <param name="settings">The <see cref="ISettings"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> cannot be null.</exception>
    public LocalConsulClientProvider(ISettings settings)
    {
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));

        settings.PropertyChanged += Settings_PropertyChanged;
        GenerateClient();
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        _Settings.PropertyChanged -= Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ConsulAddress")
            GenerateClient();
    }

    private void GenerateClient()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        Client = new ConsulClient(config => config.Address = new(_Settings.ConsulAddress));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Client)));
    }
}
