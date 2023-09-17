using Discord;
using Discord.WebSocket;

namespace Grid.Bot.Commands;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;

using Logging;
using FileSystem;
using ComputeCloud;
using Grid.Commands;
using Text.Extensions;

using Utility;
using Interfaces;
using Extensions;

/// <summary>
/// Executes a Lua script on a grid-server.
/// </summary>
[Obsolete("Text-based commands are being deprecated. Please migrate to using the /execute slash command.")]
internal class ExecuteScript : ICommandHandler
{
    /// <inheritdoc cref="ICommandHandler.Name"/>
    public string Name => "Execute Grid Server Lua Script";

    /// <inheritdoc cref="ICommandHandler.Description"/>
    public string Description => $"Attempts to execute the given script contents on a grid " +
                                        $"server instance.";

    /// <inheritdoc cref="ICommandHandler.Aliases"/>
    public string[] Aliases => new[] { "x", "ex", "execute" };

    /// <inheritdoc cref="ICommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ICommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    private const int _maxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int _maxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;

    private static readonly IGridServerFileHelper _fileHelper = new GridServerFileHelper(ArbiterSettings.Singleton);

    private (string, MemoryStream) DetermineDescription(string input, string fileName)
    {
        if (input.IsNullOrEmpty()) return (null, null);

        if (input.Length > _maxErrorLength)
        {
            var maxSize = ScriptsSettings.Singleton.ScriptExecutionMaxFileSizeKb;

            if (input.Length / 1000 > maxSize)
                return ($"The output cannot be larger than {maxSize} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

    private (string, MemoryStream) DetermineResult(string input, string fileName)
    {
        if (input.IsNullOrEmpty()) return (null, null);

        if (input.Length > _maxResultLength)
        {
            var maxSize = ScriptsSettings.Singleton.ScriptExecutionMaxResultSizeKb;

            if (input.Length / 1000 > maxSize)
                return ($"The result cannot be larger than {maxSize} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

    public async Task<bool> ParseLuaAsync(SocketMessage message, string input)
    {
        var options = new LuaParseOptions(LuaSyntaxOptions.Roblox);
        var syntaxTree = LuaSyntaxTree.ParseText(input, options);

        var diagnostics = syntaxTree.GetDiagnostics();
        var errors = diagnostics.Where(diag => diag.Severity == DiagnosticSeverity.Error);

        if (errors.Any())
        {
            var errorString = string.Join("\n", errors.Select(err => err.ToString()));

            if (errorString.Length > _maxErrorLength)
            {
                var truncated = errorString.Substring(0, _maxErrorLength - 20);

                truncated += string.Format("({0} characters remaing...)", errorString.Length - (_maxErrorLength + 20));

                errorString = truncated;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Luau Syntax Error")
                .WithAuthor(message.Author)
                .WithCurrentTimestamp()
                .WithColor(0xff, 0x00, 0x00)
                .WithDescription($"```\n{errorString}\n```")
                .Build();

            await message.ReplyAsync("There was a Luau syntax error in your script:", embed: embed);

            return false;
        }

        return true;
    }

    private async Task HandleResponseAsync(SocketMessage message, string result, LuaUtility.ReturnMetadata metadata)
    {
        var builder = new EmbedBuilder()
            .WithTitle(
                metadata.Success
                    ? "Lua Success"
                    : "Lua Error"
            )
            .WithAuthor(message.Author)
            .WithCurrentTimestamp();

        if (metadata.Success)
            builder.WithColor(0x00, 0xff, 0x00);
        else
            builder.WithColor(0xff, 0x00, 0x00);

        var (fileNameOrOutput, outputFile) = DetermineDescription(
            metadata.Logs,
            message.Id.ToString() + "-output.txt"
        );

        if (outputFile == null && !fileNameOrOutput.IsNullOrEmpty())
            builder.WithDescription($"```\n{fileNameOrOutput}\n```");

        var (fileNameOrResult, resultFile) = DetermineResult(
            metadata.Success
                ? result
                : metadata.ErrorMessage,
            message.Id.ToString() + "-result.txt"
        );

        if (resultFile == null && !fileNameOrResult.IsNullOrEmpty())
            builder.AddField("Result", $"```\n{fileNameOrResult}\n```");

        builder.AddField("Execution Time", $"{metadata.ExecutionTime:f5}s");

        var attachments = new List<FileAttachment>();
        if (outputFile != null)
            attachments.Add(new(outputFile, fileNameOrOutput));

        if (resultFile != null)
            attachments.Add(new(resultFile, fileNameOrResult));

        var text = metadata.Success
                    ? result.IsNullOrEmpty()
                        ? "Executed script with no return!"
                        : null
                    : "An error occured while executing your script:";

        if (attachments.Count > 0)
            await message.ReplyWithFilesAsync(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            await message.ReplyAsync(
                text,
                embed: builder.Build()
            );
    }

    /// <inheritdoc cref="ICommandHandler.ExecuteAsync(string[], SocketMessage, string)"/>
    public async Task ExecuteAsync(string[] contentArray, SocketMessage message, string originalCommand)
    {
        var sw = Stopwatch.StartNew();

        using (message.Channel.EnterTypingState())
        {
            var userIsAdmin = message.Author.IsAdmin();

            if (FloodCheckerRegistry.ScriptExecutionFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
            {
                await message.ReplyAsync("Too many people are using this command at once, please wait a few moments and try again.");
                return;
            }

            FloodCheckerRegistry.ScriptExecutionFloodChecker.UpdateCount();

            var perUserFloodChecker = FloodCheckerRegistry.GetPerUserScriptExecutionFloodChecker(message.Author.Id);
            if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
            {
                await message.ReplyAsync("You are sending script execution commands too quickly, please wait a few moments and try again.");
                return;
            }

            perUserFloodChecker.UpdateCount();

            var script = contentArray.Join(" ");

            if (script.IsNullOrEmpty())
            {
                // let's try and read the first attachment
                if (message.Attachments.Count == 0)
                {
                    await message.ReplyAsync("Script contents (up to 2000 chars, 4000 if nitro user), or 1 attachment was expected.");
                    return;
                }

                var firstAttachment = message.Attachments.First();
                // TODO: Setting to disable this in case we want them to use any extension
                //       because this message response can become ambigious

                if (!firstAttachment.Filename.EndsWith(".lua"))
                {
                    await message.ReplyAsync($"Expected the attachment ({firstAttachment.Filename}) to be a valid Lua file.");
                    return;
                }

                var maxSize = ScriptsSettings.Singleton.ScriptExecutionMaxFileSizeKb;

                if (firstAttachment.Size / 1000 > maxSize)
                {
                    await message.ReplyAsync($"The input attachment ({firstAttachment.Filename}) cannot be larger than {maxSize} KiB!");

                    return;
                }

                script = firstAttachment.GetAttachmentContentsAscii();
            }

            // Remove phone specific quotes (UTF-8, and Lua cannot parse them)
            script = script.EscapeQuotes();

            // Extract the script from back ticks (if they exist)
            // TODO: Skip this if we have an attachment.
            script = script.GetCodeBlockContents();

            if (script.ContainsUnicode() && !userIsAdmin)
            {
                // TODO: Ack back the UTF-8 Characters if we can in the future.
                await message.ReplyAsync("Sorry, but unicode in messages is not supported as of now, " +
                                         "please remove any unicode characters from your script.");

                return;
            }

            if (!await ParseLuaAsync(message, script)) return;

            var scriptId = Guid.NewGuid().ToString();
            var filesafeScriptId = scriptId.Replace("-", "");
            var scriptName = _fileHelper.GetGridServerScriptPath(filesafeScriptId);

            var settings = new ExecuteScriptSettings(filesafeScriptId, new Dictionary<string, object>() { { "is_admin", userIsAdmin } });
            var command = new ExecuteScriptCommand(settings);

            script = string.Format(LuaUtility.LuaVMTemplate, script);

            var scriptEx = Lua.NewScript(
                Guid.NewGuid().ToString(),
                command.ToJson()
            );

            // bump to 20 seconds so it doesn't batch job timeout on first execution
            var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };

            try
            {
                File.WriteAllText(scriptName, script, Encoding.ASCII);

                var serverResult = ScriptExecutionArbiter.Singleton.BatchJobEx(job, scriptEx);
                var (newResult, metadata) = LuaUtility.ParseResult(serverResult);

                await HandleResponseAsync(message, newResult, metadata);
            }
            catch (Exception ex)
            {
                sw.Stop();

                if (ex is IOException)
                {
                    BacktraceUtility.UploadCrashLog(ex);

                    await message.ReplyAsync("There was an IO error when writing the script to the system, please try again later.");
                }

                if (ex is TimeoutException)
                {
                    if (!message.Author.IsOwner()) message.Author.IncrementExceptionLimit();

                    await HandleResponseAsync(
                        message,
                        null,
                        new()
                        {
                            ErrorMessage = "script exceeded timeout",
                            ExecutionTime = sw.Elapsed.TotalSeconds,
                            Success = false
                        }
                    );

                    return;
                }

                if (ex is not IOException) throw;
            }
            finally
            {
                try
                {
                    Logger.Singleton.Debug(
                        "Trying delete the script '{0}' at path '{1}'",
                        scriptId,
                        scriptName
                    );
                    scriptName.PollDeletion(
                        10,
                        ex => Logger.Singleton.Warning("Failed to delete '{0}' because: {1}", scriptName, ex.Message),
                        () => Logger.Singleton.Debug(
                            "Successfully deleted the script '{0}' at path '{1}'!",
                                scriptId,
                                scriptName
                            )
                    );
                }
                catch (Exception ex)
                {
                    BacktraceUtility.UploadCrashLog(ex);

                    Logger.Singleton.Warning(
                        "Failed to delete the user script '{0}' because '{1}'",
                        scriptName,
                        ex.Message
                    );
                }
            }
        }
    }
}
