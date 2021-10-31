using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.Commands;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GridServerExecutionCommandTest : IStateSpecificCommandHandler
    {
        public string CommandName => "Grid Server Execution Command Test";
        public string CommandDescription => "A test at Lua execution to a remove grid server instance via SoapUtility.";
        public string[] CommandAliases => new string[] { "t", "test" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            using (message.Channel.EnterTypingState())
            {
                var job = new Job() { id = "Test", expirationInSeconds = 20000 };
                var script = new ScriptExecution()
                {
                    name = "Test",
                    script = new ExecuteScriptCommand(
                            new ExecuteScriptSettings(
                                "run",
                                new Dictionary<string, object>() { { "script", "return 1, 2, 3;" } }
                            )
                        ).ToJson()
                };

                var result = LuaUtility.Singleton.ParseLuaValues(await GridServerArbiter.Singleton.OpenJobExAsync(job, script));

                await message.ReplyAsync(result.IsNullOrEmpty() ? "Executed script with no return!" : $"Executed script with return:");
                if (!result.IsNullOrEmpty())
                    await message.Channel.SendMessageAsync(
                        embed: new EmbedBuilder()
                        .WithTitle("Return value")
                        .WithDescription($"```\n{result}\n```")
                        .WithAuthor(message.Author)
                        .WithCurrentTimestamp()
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
                    );
            }
        }
    }
}
