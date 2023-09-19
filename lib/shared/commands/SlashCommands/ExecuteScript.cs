#if WE_LOVE_EM_SLASH_COMMANDS

using Discord;
using Discord.WebSocket;

namespace Grid.Bot.SlashCommands;

using System;
using System.IO;
using System.Text;
using System.Linq;
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
internal class ExecuteScript : ISlashCommandHandler
{
    /// <inheritdoc cref="ISlashCommandHandler.Description"/>
    public string Description => "Execute Luau Script";

    /// <inheritdoc cref="ISlashCommandHandler.Name"/>
    public string Name => "execute";

    /// <inheritdoc cref="ISlashCommandHandler.IsInternal"/>
    public bool IsInternal => false;

    /// <inheritdoc cref="ISlashCommandHandler.IsEnabled"/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc cref="ISlashCommandHandler.Options"/>
    public SlashCommandOptionBuilder[] Options => new[]
    {
        new SlashCommandOptionBuilder()
            .WithName("attachment")
            .WithDescription("Execute a Luau script from an uploaded attachment.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("contents", ApplicationCommandOptionType.Attachment, "The Luau script attachment.", true),

        new SlashCommandOptionBuilder()
            .WithName("text")
            .WithDescription("Execute a Luau script directly on the command line.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("contents", ApplicationCommandOptionType.String, "The Luau script contents.", true)
    };

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

    public async Task<bool> ParseLuaAsync(SocketSlashCommand command, string input)
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
                .WithAuthor(command.User)
                .WithCurrentTimestamp()
                .WithColor(0xff, 0x00, 0x00)
                .WithDescription($"```\n{errorString}\n```")
                .Build();

            await command.RespondPublicAsync("There was a Luau syntax error in your script:", embed: embed);

            return false;
        }

        return true;
    }

    private async Task HandleResponseAsync(SocketSlashCommand command, string result, LuaUtility.ReturnMetadata metadata)
    {
        var builder = new EmbedBuilder()
            .WithTitle(
                metadata.Success
                    ? "Lua Success"
                    : "Lua Error"
            )
            .WithAuthor(command.User)
            .WithCurrentTimestamp();

        if (metadata.Success)
            builder.WithColor(0x00, 0xff, 0x00);
        else
            builder.WithColor(0xff, 0x00, 0x00);

        var (fileNameOrOutput, outputFile) = DetermineDescription(
            metadata.Logs,
            command.Id.ToString() + "-output.txt"
        );

        if (outputFile == null && !fileNameOrOutput.IsNullOrEmpty())
            builder.WithDescription($"```\n{fileNameOrOutput}\n```");

        var (fileNameOrResult, resultFile) = DetermineResult(
            metadata.Success
                ? result
                : metadata.ErrorMessage,
            command.Id.ToString() + "-result.txt"
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
            await command.RespondWithFilesPublicAsync(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            await command.RespondPublicAsync(
                text,
                embed: builder.Build()
            );
    }

    private static async Task<(bool, string)> GetScriptContents(
        SocketSlashCommand item,
        SocketSlashCommandDataOption subcommand
    )
    {
        var contents = "";

        switch (subcommand.Name.ToLower())
        {
            case "attachment":
                var attachment = subcommand.GetOptionValue<IAttachment>("contents");
                if (attachment == null)
                {
                    await item.RespondEphemeralPingAsync("The attachment is required.");
                    return (false, contents);
                }

                if (!attachment.Filename.EndsWith(".lua"))
                {
                    await item.RespondEphemeralPingAsync($"Expected the attachment ({attachment.Filename}) to be a valid Lua file.");
                    return (false, contents);
                }

                var maxSize = ScriptsSettings.Singleton.ScriptExecutionMaxFileSizeKb;

                if (attachment.Size / 1000 > maxSize)
                {
                    await item.RespondEphemeralPingAsync($"The input attachment ({attachment.Filename}) cannot be larger than {maxSize} KiB!");
                    return (false, contents);
                }

                contents = attachment.GetAttachmentContentsAscii().EscapeQuotes();

                return (true, contents);
            case "text":
                contents = subcommand.GetOptionValue<string>("contents")?.GetCodeBlockContents();

                return (true, contents);
        }

        return (false, contents);
    }

    /// <inheritdoc cref="ISlashCommandHandler.ExecuteAsync(SocketSlashCommand)"/>
    public async Task ExecuteAsync(SocketSlashCommand command)
    {
        var sw = Stopwatch.StartNew();

        var userIsAdmin = command.User.IsAdmin();

        if (FloodCheckerRegistry.ScriptExecutionFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
        {
            await command.RespondEphemeralAsync("Too many people are using this command at once, please wait a few moments and try again.");

            return;
        }

        FloodCheckerRegistry.ScriptExecutionFloodChecker.UpdateCount();

        var perUserFloodChecker = FloodCheckerRegistry.GetPerUserScriptExecutionFloodChecker(command.User.Id);
        if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
        {
            await command.RespondEphemeralAsync("You are sending script execution commands too quickly, please wait a few moments and try again.");

            return;
        }

        perUserFloodChecker.UpdateCount();

        var subcommand = command.Data.GetSubCommand();

        var (success, script) = await GetScriptContents(command, subcommand);
        if (!success) return;

        if (script.IsNullOrEmpty())
        {
            await command.RespondEphemeralPingAsync("Raw script contents, or 1 attachment was expected.");

            return;
        }

        // Remove phone specific quotes (UTF-8, and Lua cannot parse them)
        script = script.EscapeQuotes();

        // Extract the script from back ticks (if they exist)
        // TODO: Skip this if we have an attachment.
        script = script.GetCodeBlockContents();

        if (script.ContainsUnicode() && !userIsAdmin)
        {
            // TODO: Ack back the UTF-8 Characters if we can in the future.
            await command.RespondEphemeralPingAsync("Sorry, but unicode in messages is not supported as of now, " +
                                                    "please remove any unicode characters from your script.");
            return;
        }

        if (!await ParseLuaAsync(command, script))
            return;

        var scriptId = Guid.NewGuid().ToString();
        var filesafeScriptId = scriptId.Replace("-", "");
        var scriptName = _fileHelper.GetGridServerScriptPath(filesafeScriptId);

        // isAdmin allows a bypass of disabled methods and virtualized globals
        var settings = new ExecuteScriptSettings(filesafeScriptId, new Dictionary<string, object>() { { "is_admin", userIsAdmin } });
        var gserverCommand = new ExecuteScriptCommand(settings);

        script = string.Format(LuaUtility.LuaVMTemplate, script);

        var scriptEx = Lua.NewScript(
            Guid.NewGuid().ToString(),
            gserverCommand.ToJson()
        );

        // bump to 20 seconds so it doesn't batch job timeout on first execution
        var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };

        try
        {
            File.WriteAllText(scriptName, script, Encoding.ASCII);

            var serverResult = ScriptExecutionArbiter.Singleton.BatchJobEx(job, scriptEx);
            var (result, metadata) = LuaUtility.ParseResult(serverResult);

            await HandleResponseAsync(command, result, metadata);
        }
        catch (Exception ex)
        {
            if (ex is IOException)
            {
                BacktraceUtility.UploadCrashLog(ex);

                await command.RespondEphemeralPingAsync("There was an IO error when writing the script to the system, please try again later.");
            }

            if (ex is TimeoutException)
            {
                if (!command.User.IsOwner()) command.User.IncrementExceptionLimit();

                await HandleResponseAsync(command, null, new() { ErrorMessage = "script exceeded timeout", ExecutionTime = sw.Elapsed.TotalSeconds, Success = false });

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

#endif
