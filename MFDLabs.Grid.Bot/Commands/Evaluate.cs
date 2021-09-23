using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class Evaluate : IStateSpecificCommandHandler
    {
        public string CommandName => "Evaluate";
        public string CommandDescription => "Evaluates the given C# code with error handling included.";
        public string[] CommandAliases => new string[] { "eval" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotOwnerAsync()) return;

            var scriptContents = messageContentArray.Join(' ');
            if (scriptContents.IsNullWhiteSpaceOrEmpty())
            {
                SystemLogger.Singleton.Warning("The script was null or empty, aborting.");
                await message.ReplyAsync("The script is required.");
                return;
            }
            scriptContents = scriptContents.EscapeQuotes().Replace("`", "");

            ScriptState<object> result;

            using (message.Channel.EnterTypingState())
            {

                try
                {
                    // ref the current assembly
                    result = await CSharpScript.RunAsync(scriptContents, ScriptOptions.Default.WithReferences(Assembly.GetExecutingAssembly()));
                }
                catch (CompilationErrorException ex)
                {
                    await HandleException(message, ex);
                    return;
                }
                catch (Exception ex)
                {
                    await HandleException(message, ex);
                    return;
                }

                await message.ReplyAsync(result.ReturnValue == null ? "Executed script with no return!" : $"Executed script with return:");
                if (result.ReturnValue != null)
                    await message.Channel.SendMessageAsync(
                        embed: new EmbedBuilder()
                        .WithTitle("Return value")
                        .WithDescription($"```\n{result.ReturnValue}\n```")
                        .WithAuthor(message.Author)
                        .WithCurrentTimestamp()
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
                    );
            }
        }

        private async Task HandleException(SocketMessage message, Exception ex)
        {
            await message.ReplyAsync($"an exception occurred when trying to execute the given C#, please review this error to see if your input was malformed:");
            await message.Channel.SendMessageAsync(
                embed: new EmbedBuilder()
                .WithColor(0xff, 0x00, 0x00)
                .WithTitle("Execute exception.")
                .WithAuthor(message.Author)
                .WithDescription($"```\n{ex.Message}\n```")
                .Build()
            );
        }
    }
}
