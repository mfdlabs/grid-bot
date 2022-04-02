#if WE_ON_THE_GRID
using System.Configuration;
using MFDLabs.Configuration.Providers;
#endif

#if WE_ON_THE_GRID
namespace MFDLabs.Grid.AutoDeployer.Properties
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