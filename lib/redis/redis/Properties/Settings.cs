#if USE_VAULT_SETTINGS_PROVIDER
using Configuration.Providers;
#endif

namespace Redis.Properties
{
#if USE_VAULT_SETTINGS_PROVIDER
    [SettingsProvider(typeof(VaultProvider))]
#endif
    internal sealed partial class Settings
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
