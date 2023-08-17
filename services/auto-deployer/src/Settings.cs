#if USE_VAULT_SETTINGS_PROVIDER
using System.Configuration;
using Configuration.Providers;

namespace Grid.AutoDeployer.Properties
{
    [SettingsProvider(typeof(VaultProvider))]
    internal sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            VaultProvider.Register(e, this);
        }
    }
}
#endif