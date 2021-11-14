﻿using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Abstractions;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Global
{
    public sealed class BotGlobal : SingletonBase<BotGlobal>
    {

        private DiscordSocketClient _client;

        public DiscordSocketClient Client { get { return _client; } }

        internal void Initialize(DiscordSocketClient client)
        {
            _client = client;
        }

        internal async Task SingletonLaunch()
        {
            await _client.LoginAsync(TokenType.Bot, global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotToken).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);
        }

        internal async Task TryLogout()
        {
            SystemLogger.Singleton.Log("Attempting to logout bot user...");

            if (_client != null)
            {
                try
                {
                    await _client.LogoutAsync().ConfigureAwait(false);
                    await _client.StopAsync().ConfigureAwait(false);
                    SystemLogger.Singleton.Info("Bot successfully logged out and stopped!");
                }
                catch
                {
                    SystemLogger.Singleton.Warning("Failed to log out bot user, most likely wasn't logged in prior.");
                }
            }
        }
    }
}
