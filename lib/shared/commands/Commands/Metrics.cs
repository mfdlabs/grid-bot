using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Registries;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Metrics : IStateSpecificCommandHandler
    {
        public string CommandName => "Get bot metrics";
        public string CommandDescription => "Gets an embed of the current metrics for the bot instance.";
        public string[] CommandAliases => new[] { "metrics", "stats" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var (modes, counters) = CommandRegistry.GetMetrics();

            var metricsPort = global::MFDLabs.Grid.Bot.Properties.Settings.Default.CounterServerPort;

            var embed = new EmbedBuilder()
                .WithTitle("Bot Instance Metrics")
                .WithUrl($"http://{SystemGlobal.GetMachineHost()}:{metricsPort}/")
                .WithCurrentTimestamp()
                .AddField(
                    "Counters",
                    $"Total: {counters.RequestCountN}\nSucceeded: {counters.RequestSucceededCountN}\nFailed: {counters.RequestFailedCountN}",
                    true
                )
                .AddField(
                    "Modes",
                    $"Average user: {modes.Users.Item} at {modes.Users.Average}\nAverage guild: {modes.Servers.Item} at {modes.Servers.Average}\nAverage command: {modes.Commands.Item} at {modes.Commands.Average}",
                    true
                )
                .Build();

            await message.ReplyAsync(
                $"Command Registry metrics report for at ({DateTimeGlobal.GetUtcNowAsIso()}",
                embed: embed
            );
        }
    }
}
