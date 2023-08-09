using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grid.Bot.Extensions;
using Grid.Bot.Interfaces;
using Text.Extensions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Grid.Bot.Commands
{
    internal sealed class EvaluateScript : IStateSpecificCommandHandler
    {
        public string CommandName => "Evaluate Runtime CSharp Script";
        public string CommandDescription => "Attempts to evaluate a script with the given name on the " +
                                            $"current machine\nLayout: {Grid.Bot.Properties.Settings.Default.Prefix}evauluatescript scriptName.";
        public string[] CommandAliases => new[] { "evals", "evalscript", "evauluatescript" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotOwnerAsync()) return;

            var scriptName = messageContentArray.ElementAtOrDefault(0);
            if (scriptName.IsNullWhiteSpaceOrEmpty())
            {
                await message.ReplyAsync("The script name is required.");
                return;
            }

            scriptName = scriptName.Escape().EscapeNewLines().EscapeQuotes().Replace("..", "");

            
            
            var fullScriptName = Path.Combine(Directory.GetCurrentDirectory(), "RuntimeScripts", $"{scriptName}.csx");

            if (!File.Exists(fullScriptName))
            {
                await message.ReplyAsync($"Unable to find the script '{scriptName}' at the specified path '{fullScriptName}'.");
                return;
            }

            await message.Channel.SendMessageAsync($"Executing script '{scriptName}' at path '{fullScriptName}'.");

            using (message.Channel.EnterTypingState())
            {
                ScriptState<object> result;
                try
                {
                    // ref the current assembly
                    result = await CSharpScript.RunAsync(
                        $"#load \"{fullScriptName}\"",
                        ScriptOptions.Default
                            .WithReferences(Assembly.GetExecutingAssembly())
                            .WithImports(
                                "System",
                                "System.Linq",
                                "System.Threading.Tasks",
                                "System.Collections.Generic",

                                "Discord",
                                "Discord.WebSocket",

                                "Grid",
                                "Grid.Bot.Extensions",
                                "Text.Extensions"
                            ),
                        new Globals { messageContentArray = messageContentArray, message = message, originalCommand = originalCommand }
                    );
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

                
                if (result.ReturnValue != null)
                {
                    if (result.ReturnValue.ToString().Length > EmbedBuilder.MaxDescriptionLength)
                    {
                        await message.ReplyWithFileAsync(
                            new MemoryStream(Encoding.UTF8.GetBytes(result.ReturnValue.ToString())),
                            "eval.txt",
                            "Executed script with return:");
                        return;
                    }
                    await message.Channel.SendMessageAsync(
                        "Executed script with return:",
                        embed: new EmbedBuilder()
                        .WithTitle("Return value")
                        .WithDescription($"```\n{result.ReturnValue}\n```")
                        .WithAuthor(message.Author)
                        .WithCurrentTimestamp()
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
                    );
                    return;
                }
                await message.ReplyAsync("Executed script with no return!");
            }
        }

        private static async Task HandleException(SocketMessage message, Exception ex)
        {
            if (ex.Message.Length > EmbedBuilder.MaxDescriptionLength)
            {
                await message.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(ex.Message)),
                    "eval-error.txt",
                    "an exception occurred when trying to execute the given C#, please review this error to see if your input was malformed:");
                return;
            }

            await message.Channel.SendMessageAsync(
                "an exception occurred when trying to execute the given C#, please review this error to see if your input was malformed:",
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
