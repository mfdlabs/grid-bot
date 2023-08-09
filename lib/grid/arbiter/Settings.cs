#if USE_VAULT_SETTINGS_PROVIDER
using System.Configuration;
using Configuration.Providers;
#endif

#if USE_VAULT_SETTINGS_PROVIDER
namespace Grid.Properties
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
