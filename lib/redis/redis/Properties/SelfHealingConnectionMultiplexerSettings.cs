#if USE_VAULT_SETTINGS_PROVIDER
using Configuration.Providers;
#endif

namespace Redis.Properties
{
    /// <summary>
    /// Configuration provider using Vault
    /// </summary>
#if USE_VAULT_SETTINGS_PROVIDER
    [SettingsProvider(typeof(VaultProvider))]
#endif
    public sealed partial class SelfHealingConnectionMultiplexerSettings : ISelfHealingConnectionMultiplexerSettings
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
