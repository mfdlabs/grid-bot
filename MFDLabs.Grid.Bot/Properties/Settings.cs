#if WE_ON_THE_GRID
using System.Configuration;
using MFDLabs.Configuration.Providers;

namespace MFDLabs.Grid.Bot.Properties
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
#elif WE_ON_THE_RUN
using System.Configuration;
using MFDLabs.Configuration.Providers;

namespace MFDLabs.Grid.Bot.Properties
{
    [SettingsProvider(typeof(RemoteServiceProvider))]
    internal sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            RemoteServiceProvider.Register(e, this);
        }
    }
}
#elif WE_ARE_AN_ACTOR
using System.Configuration;
using MFDLabs.Configuration.Providers;

namespace MFDLabs.Grid.Bot.Properties
{
    [SettingsProvider(typeof(DataBaseProvider))]
    internal sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            base.OnSettingsLoaded(sender, e);
            DataBaseProvider.Register(e, this);
        }
    }
}
#endif