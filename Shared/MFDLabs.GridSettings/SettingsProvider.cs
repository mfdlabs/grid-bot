/* Copyright MFDLABS Corporation. All rights reserved. */

using Discord.WebSocket;

// WE_LOVE_THE_GRID_CONFIG -> Guild Based configuration, experimental, only debug builds have it

#if WE_ON_THE_GRID && WE_LOVE_THE_GRID_CONFIG
using MFDLabs.Discord.Configuration;
#endif

namespace MFDLabs.Grid.Bot.Properties
{
    public static class SettingsProvider
    {
#if WE_ON_THE_GRID && WE_LOVE_THE_GRID_CONFIG
        private const string SettingsGroupName = "MFDLabs.Grid.Bot.Properties.Settings";

        static SettingsProvider()
        {
            DiscordConfigurationHelper.InitializeClient(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.DiscordConfigurationVaultAddress,
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.DiscordConfigurationVaultToken
            );
        }
#endif

        public static T GetSetting<T>(this SocketMessage message, string settingName)
        {
#if WE_ON_THE_GRID && WE_LOVE_THE_GRID_CONFIG
            return message.GetSetting<T>(SettingsGroupName, settingName);
#else
            return (T)global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName];
#endif
        }

        public static void WriteSetting<T>(this SocketMessage message, string settingName, T value)
        {
#if WE_ON_THE_GRID && WE_LOVE_THE_GRID_CONFIG
            message.WriteSetting<T>(SettingsGroupName, settingName, value);
#else
            global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName] = value;
            global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
#endif
        }
    }
}