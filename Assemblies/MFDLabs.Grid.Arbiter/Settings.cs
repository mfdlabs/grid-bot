﻿#if WE_ON_THE_GRID || WE_ON_THE_RUN || WE_ARE_AN_ACTOR
using System.Configuration;
using MFDLabs.Configuration.Providers;
#endif

#if WE_ON_THE_GRID
namespace MFDLabs.Grid.Properties
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
namespace MFDLabs.Grid.Properties
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
namespace MFDLabs.Grid.Properties
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