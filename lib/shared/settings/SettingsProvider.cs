using Discord.WebSocket;

// WE_LOVE_THE_GRID_CONFIG -> Guild Based configuration, experimental, only debug builds have it

#if USE_VAULT_SETTINGS_PROVIDER && WE_LOVE_THE_GRID_CONFIG
using Discord.Configuration;
#endif

namespace Grid.Bot.Properties
{
    public static class SettingsProvider
    {
#if USE_VAULT_SETTINGS_PROVIDER && WE_LOVE_THE_GRID_CONFIG
        private const string SettingsGroupName = "Grid.Bot.Properties.Settings";

        static SettingsProvider()
        {
            DiscordConfigurationHelper.InitializeClient(
                global::Grid.Bot.Properties.Settings.Default.DiscordConfigurationVaultAddress,
                global::Grid.Bot.Properties.Settings.Default.DiscordConfigurationVaultToken
            );
        }
#endif

        public static T GetSetting<T>(this SocketMessage message, string settingName)
        {
#if USE_VAULT_SETTINGS_PROVIDER && WE_LOVE_THE_GRID_CONFIG
            return message.GetSetting<T>(SettingsGroupName, settingName);
#else
            return (T)global::Grid.Bot.Properties.Settings.Default[settingName];
#endif
        }

        public static void WriteSetting<T>(this SocketMessage message, string settingName, T value)
        {
#if USE_VAULT_SETTINGS_PROVIDER && WE_LOVE_THE_GRID_CONFIG
            message.WriteSetting<T>(SettingsGroupName, settingName, value);
#else
            global::Grid.Bot.Properties.Settings.Default[settingName] = value;
            global::Grid.Bot.Properties.Settings.Default.Save();
#endif
        }
    }
}