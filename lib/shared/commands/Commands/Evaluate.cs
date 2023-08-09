﻿using System;
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
    public class Globals
    {
        public string[] messageContentArray { get; internal set; }
        public SocketMessage message { get; internal set; }
        public string originalCommand { get; internal set; }
        public string scriptContents { get; internal set; }
    }

    internal sealed class Evaluate : IStateSpecificCommandHandler
    {
        public string CommandName => "Evaluate CSharp";
        public string CommandDescription => "Attempts to evaluate the given C# with Roslyn\nLayout:" +
                                            $"{(global::Grid.Bot.Properties.Settings.Default.Prefix)}evaluate " +
                                            "...scriptContents.";
        public string[] CommandAliases => new[] { "eval", "evaluate" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;


        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotOwnerAsync()) return;

            var scriptContents = messageContentArray.Join(' ');
            if (scriptContents.IsNullWhiteSpaceOrEmpty())
            {
                // let's try and read the attachments.
                if (message.Attachments.Count == 0)
                {
                    await message.ReplyAsync("The script or at least 1 attachment was expected.");
                    return;
                }

                var firstAttachment = message.Attachments.First();

                scriptContents = firstAttachment.GetAttachmentContentsAscii();
            }

            scriptContents = scriptContents.EscapeQuotes().GetCodeBlockContents();

            using (message.Channel.EnterTypingState())
            {
                ScriptState<object> result;
                try
                {
                    // ref the current assembly
                    result = await CSharpScript.RunAsync(
                        scriptContents,
                        ScriptOptions.Default
                            .WithReferences(Assembly.GetExecutingAssembly())
                            .WithAllowUnsafe(true)
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
                        new Globals { messageContentArray = messageContentArray, message = message, originalCommand = originalCommand, scriptContents = scriptContents }
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
                await message.ReplyWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(ex.Message)),
                    "eval-error.txt",
                    "An exception occurred when trying to execute the given C#, please review this error to see " +
                    "if your input was malformed:");
                return;
            }

            await message.ReplyAsync(
                "An exception occurred when trying to execute the given C#, please review this error to see " +
                "if your input was malformed:",
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
