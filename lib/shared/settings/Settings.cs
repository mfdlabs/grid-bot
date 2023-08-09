#if USE_VAULT_SETTINGS_PROVIDER || WE_ON_THE_RUN || WE_ARE_AN_ACTOR
using System.Configuration;
using Configuration.Providers;
#endif

#if USE_VAULT_SETTINGS_PROVIDER
namespace Grid.Bot.Properties
{
    [SettingsProvider(typeof(VaultProvider))]
    public sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            VaultProvider.Register(e, this);
        }
    }
}
#elif WE_ON_THE_RUN
namespace Grid.Bot.Properties
{
    [SettingsProvider(typeof(RemoteServiceProvider))]
    public sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            RemoteServiceProvider.Register(e, this);
        }
    }
}
#elif WE_ARE_AN_ACTOR
namespace Grid.Bot.Properties
{
    [SettingsProvider(typeof(DataBaseProvider))]
    public sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            DataBaseProvider.Register(e, this);
        }
    }
}
#endif