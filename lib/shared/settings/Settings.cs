#if USE_VAULT_SETTINGS_PROVIDER
using System.Configuration;
using Configuration.Providers;
#endif

using IServiceDiscoverySettings = global::ServiceDiscovery.Properties.ISettings;

namespace Grid.Bot.Properties
{
#if USE_VAULT_SETTINGS_PROVIDER
    [SettingsProvider(typeof(VaultProvider))]
#endif
    public sealed partial class Settings : IServiceDiscoverySettings
    {
#if USE_VAULT_SETTINGS_PROVIDER
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            VaultProvider.Register(e, this);
        }
#endif
    }
}
