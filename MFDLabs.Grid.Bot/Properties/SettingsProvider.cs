using Discord.WebSocket;

#if WE_ON_THE_GRID
using MFDLabs.Discord.Configuration;
#endif

namespace MFDLabs.Grid.Bot.Properties
{
    public static class SettingsProvider
    {
#if WE_ON_THE_GRID
        static SettingsProvider()
        {
            DiscordConfigurationHelper.InitializeClient(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.DiscordConfigurationVaultAddress,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.DiscordConfigurationVaultToken);
        }
#endif

        public static T GetSetting<T>(this SocketMessage message, string settingName)
        {
#if WE_ON_THE_GRID
            return message.GetSetting<T>("MFDLabs.Grid.Bot.Properties.Settings", settingName);
#else
            return (T)global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName];
#endif
        }

        public static void WriteSetting<T>(this SocketMessage message, string settingName, T value)
        {
#if WE_ON_THE_GRID
            message.WriteSetting<T>("MFDLabs.Grid.Bot.Properties.Settings", settingName, value);
#else
            global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName] = value;
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
#endif
        }
    }
}