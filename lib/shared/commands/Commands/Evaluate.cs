namespace Grid.Bot.Commands;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

using Text.Extensions;

using Extensions;
using Interfaces;

#pragma warning disable IDE1006 // Naming Styles

/// <summary>
/// Globals class used by <see cref="Evaluate"/>
/// </summary>
/// <remarks>
/// This needs to public due to how CA handles data.
/// </remarks>
[Obsolete("Text-based commands are being deprecated. Please begin to use slash commands!")]
public class EvaluteCommandGlobals
{
    /// <summary>
    /// The arguments.
    /// </summary>
    public string[] messageContentArray { get; internal set; }

    /// <summary>
    /// The raw <see cref="SocketMessage"/>
    /// </summary>
    public SocketMessage message { get; internal set; }

    /// <summary>
    /// The original command name for alias analysis.
    /// </summary>
    public string originalCommand { get; internal set; }

    /// <summary>
    /// The raw contents of the C# script.
    /// </summary>
    public string scriptContents { get; internal set; }
}

#pragma warning restore IDE1006 // Naming Styles

/// <summary>
/// Evaluates a C# string or file.
/// </summary>
[Obsolete("Text-based commands are being deprecated.")]
internal sealed class Evaluate : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Evaluate CSharp";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => "Attempts to evaluate the given C# with Roslyn\nLayout:" +
                                        $"{(CommandsSettings.Singleton.Prefix)}evaluate " +
                                        "...scriptContents.";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "eval", "evaluate" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => true;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] messageContentArray, SocketMessage message, string originalCommand)
    {
        if (!await message.RejectIfNotOwnerAsync()) return;

        var scriptContents = messageContentArray.Join(' ');
        if (scriptContents.IsNullOrEmpty())
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

                            "Text.Extensions",

                            "Grid",
                            "Grid.Bot.Extensions",
                            "Grid.Bot.Utility"
                        ),
                    new EvaluteCommandGlobals
                    {
                        messageContentArray = messageContentArray,
                        message = message,
                        originalCommand = originalCommand,
                        scriptContents = scriptContents
                    }
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
            await message.ReplyWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(ex.Message)),
                "eval-error.txt",
                "An exception occurred when trying to execute the given C#, please review this error to see " +
                "if your input was malformed:"
            );

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
