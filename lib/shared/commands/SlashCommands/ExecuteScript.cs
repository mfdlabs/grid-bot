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
using Networking;
using Diagnostics;
using ComputeCloud;
using Text.Extensions;
using Instrumentation;

using Utility;
using Interfaces;
using Extensions;
using PerformanceMonitors;

internal class ExecuteScript : IStateSpecificSlashCommandHandler
{
    public string CommandDescription => "Execute Luau Script";
    public string Name => "execute";
    public bool Internal => false;
    public bool IsEnabled { get; set; } = true;

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

    private sealed class ExecuteScriptSlashCommandPerformanceMonitor
    {
        private const string Category = "Grid.SlashCommands.ExecuteScript";

        public IRawValueCounter TotalItemsProcessed { get; }
        public IRawValueCounter TotalItemsProcessedThatFailed { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadEmptyScripts { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadEmptyScriptsPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachmentPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachmentPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadAnInvalidScriptFile { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadAnInvalidScriptFilePerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadBlacklistedKeywords { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadBlacklistedKeywordsPerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadUnicode { get; }
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadUnicodePerSecond { get; }
        public IRawValueCounter TotalItemsProcessedThatHadAFileResult { get; }
        public IAverageValueCounter ExecuteScriptSlashCommandSuccessAverageTimeTicks { get; }
        public IAverageValueCounter ExecuteScriptSlashCommandFailureAverageTimeTicks { get; }

        public ExecuteScriptSlashCommandPerformanceMonitor(ICounterRegistry counterRegistry)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

            var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

            TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
            TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
            TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatFailedPerSecond", instance);
            TotalItemsProcessedThatHadEmptyScripts = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadEmptyScripts", instance);
            TotalItemsProcessedThatHadEmptyScriptsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsPerSecond", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachmentPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachmentPerSecond", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment", instance);
            TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachmentPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachmentPerSecond", instance);
            TotalItemsProcessedThatHadAnInvalidScriptFile = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadAnInvalidScriptFile", instance);
            TotalItemsProcessedThatHadAnInvalidScriptFilePerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadAnInvalidScriptFilePerSecond", instance);
            TotalItemsProcessedThatHadBlacklistedKeywords = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadBlacklistedKeywords", instance);
            TotalItemsProcessedThatHadBlacklistedKeywordsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadBlacklistedKeywordsPerSecond", instance);
            TotalItemsProcessedThatHadUnicode = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadUnicode", instance);
            TotalItemsProcessedThatHadUnicodePerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadUnicodePerSecond", instance);
            TotalItemsProcessedThatHadAFileResult = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadAFileResult", instance);
            ExecuteScriptSlashCommandSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ExecuteScriptSlashCommandSuccessAverageTimeTicks", instance);
            ExecuteScriptSlashCommandFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ExecuteScriptSlashCommandFailureAverageTimeTicks", instance);
        }
    }

    private const int MaxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int MaxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;

    #region Metrics

    private static readonly ExecuteScriptSlashCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

    #endregion Metrics

    private (string, MemoryStream) DetermineDescription(string input, string fileName)
    {
        if (input.IsNullOrEmpty()) return (null, null);

        if (input.Length > MaxErrorLength)
        {
            if (input.Length / 1000 > global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxFileSizeKb)
                return ($"The output cannot be larger than {(global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxResultSizeKb)} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }

    private (string, MemoryStream) DetermineResult(string input, string fileName)
    {
        if (input.IsNullOrEmpty()) return (null, null);

        if (input.Length > MaxResultLength)
        {
            if (input.Length / 1000 > global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxResultSizeKb)
                return ($"The result cannot be larger than {(global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxResultSizeKb)} KiB", null);

            return (fileName, new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        return (input, null);
    }


    public bool ParseLua(SocketSlashCommand command, string input)
    {
        var options = new LuaParseOptions(LuaSyntaxOptions.Roblox);
        var syntaxTree = LuaSyntaxTree.ParseText(input, options);

        var diagnostics = syntaxTree.GetDiagnostics();
        var errors = diagnostics.Where(diag => diag.Severity == DiagnosticSeverity.Error);

        if (errors.Any())
        {
            var errorString = string.Join("\n", errors.Select(err => err.ToString()));

            if (errorString.Length > MaxErrorLength)
            {
                var truncated = errorString.Substring(0, MaxErrorLength - 20);

                truncated += string.Format("({0} characters remaing...)", errorString.Length - (MaxErrorLength + 20));

                errorString = truncated;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Luau Syntax Error")
                .WithAuthor(command.User)
                .WithCurrentTimestamp()
                .WithColor(0xff, 0x00, 0x00)
                .WithDescription($"```\n{errorString}\n```")
                .Build();

            command.RespondPublic("There was a Luau syntax error in your script:", embed: embed);

            return false;
        }

        return true;
    }

    private void HandleResponse(SocketSlashCommand command, string result, LuaUtility.ReturnMetadata metadata)
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
            command.RespondWithFilesPublic(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            command.RespondPublic(
                text,
                embed: builder.Build()
            );
    }

    private static bool GetScriptContents(
        ref SocketSlashCommand item,
        ref SocketSlashCommandDataOption subcommand,
        ref bool isFailure,
        out string contents
    )
    {
        contents = null;

        switch (subcommand.Name.ToLower())
        {
            case "attachment":
                var attachment = (IAttachment)subcommand.GetOptionValue("contents");
                if (attachment == null)
                {
                    isFailure = true;

                    item.RespondEphemeralPing("The attachment is required.");
                    return false;
                }

                if (!attachment.Filename.EndsWith(".lua"))
                {
                    _perfmon.TotalItemsProcessedThatHadAnInvalidScriptFile.Increment();
                    _perfmon.TotalItemsProcessedThatHadAnInvalidScriptFilePerSecond.Increment();
                    isFailure = true;

                    item.RespondEphemeralPing($"Expected the attachment ({attachment.Filename}) to be a valid Lua file.");
                    return false;
                }

                if (attachment.Size / 1000 > global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxFileSizeKb)
                {
                    isFailure = true;

                    item.RespondEphemeralPing($"The input attachment ({attachment.Filename}) cannot be larger than {(global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxFileSizeKb)} KiB!");
                    return false;
                }

                _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment.Increment();
                _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachmentPerSecond.Increment();

                contents = attachment.GetAttachmentContentsAscii().EscapeQuotes();

                return true;
            case "text":
                contents = subcommand.GetOptionValue("contents")?.ToString()?.GetCodeBlockContents();
                return true;
        }

        return false;
    }

    public async Task Invoke(SocketSlashCommand command)
    {
        _perfmon.TotalItemsProcessed.Increment();

        var sw = Stopwatch.StartNew();
        var isFailure = false;

        try
        {
            var userIsAdmin = command.User.IsAdmin();

            if (FloodCheckerRegistry.ScriptExecutionFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
            {
                await command.RespondEphemeralAsync("Too many people are using this command at once, please wait a few moments and try again.");
                isFailure = true;
                return;
            }

            FloodCheckerRegistry.ScriptExecutionFloodChecker.UpdateCount();

            var perUserFloodChecker = FloodCheckerRegistry.GetPerUserScriptExecutionFloodChecker(command.User.Id);
            if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
            {
                await command.RespondEphemeralAsync("You are sending script execution commands too quickly, please wait a few moments and try again.");
                isFailure = true;
                return;
            }

            perUserFloodChecker.UpdateCount();

            var subcommand = command.Data.GetSubCommand();

            if (!GetScriptContents(ref command, ref subcommand, ref isFailure, out var script)) return;

            if (script.IsNullWhiteSpaceOrEmpty())
            {
                _perfmon.TotalItemsProcessedThatHadEmptyScripts.Increment();
                _perfmon.TotalItemsProcessedThatHadEmptyScriptsPerSecond.Increment();

                _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment.Increment();
                _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachmentPerSecond.Increment();

                isFailure = true;
                await command.RespondEphemeralPingAsync("Raw script contents, or 1 attachment was expected.");
                return;
            }

            // Remove phone specific quotes (UTF-8, and Lua cannot parse them)
            script = script.EscapeQuotes();

            // Extract the script from back ticks (if they exist)
            // TODO: Skip this if we have an attachment.
            script = script.GetCodeBlockContents();

            if (LuaUtility.CheckIfScriptContainsDisallowedText(script, out var keyword) && !userIsAdmin)
            {
                _perfmon.TotalItemsProcessedThatHadBlacklistedKeywords.Increment();
                _perfmon.TotalItemsProcessedThatHadBlacklistedKeywordsPerSecond.Increment();

                isFailure = true;

                await command.RespondEphemeralPingAsync($"The script you sent contains keywords that are not permitted, " +
                                                        $"please review your script and change the blacklisted keyword: {keyword}");

                return;
            }

            if (script.ContainsUnicode() && !global::Grid.Bot.Properties.Settings.Default.ScriptExecutionSupportUnicode && !userIsAdmin)
            {
                _perfmon.TotalItemsProcessedThatHadUnicode.Increment();
                _perfmon.TotalItemsProcessedThatHadUnicodePerSecond.Increment();

                isFailure = true;

                // TODO: Ack back the UTF-8 Characters if we can in the future.
                await command.RespondEphemeralPingAsync("Sorry, but unicode in messages is not supported as of now, " +
                                                        "please remove any unicode characters from your script.");
                return;
            }

            if (!ParseLua(command, script))
            {
                isFailure = true;
                return;
            }

            var isAdminScript = global::Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

            var scriptId = NetworkingGlobal.GenerateUuidv4();
            var filesafeScriptId = scriptId.Replace("-", "");
            var scriptName = GridServerFileHelper.GetGridServerScriptPath(filesafeScriptId);

            // isAdmin allows a bypass of disabled methods and virtualized globals
            var (gserverCommand, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
                filesafeScriptId,
                ("is_admin", isAdminScript)
            );

            if (isAdminScript) Logger.Singleton.Debug("Admin scripts are enabled, disabling VM.");

            if (global::Grid.Bot.Properties.Settings.Default.ScriptExecutionRequireProtections)
                script = string.Format(LuaUtility.SafeLuaMode, script);

            if (global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPrependBaseURL)
                script = $"game:GetService(\"ContentProvider\"):SetBaseUrl" +
                         $"(\"{Grid.Bot.Properties.Settings.Default.BaseURL}\");{script}";

            var scriptEx = Lua.NewScript(
                NetworkingGlobal.GenerateUuidv4(),
                gserverCommand
            );

            // bump to 20 seconds so it doesn't batch job timeout on first execution
            var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };

            try
            {
                File.WriteAllText(scriptName, script, Encoding.ASCII);

                var serverResult = ScriptExecutionArbiter.Singleton.BatchJobEx(job, scriptEx);
                var (result, metadata) = LuaUtility.ParseResult(serverResult);

                HandleResponse(command, result, metadata);
            }
            catch (Exception ex)
            {
                isFailure = true;

                if (ex is IOException)
                {
                    global::Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                    await command.RespondEphemeralPingAsync("There was an IO error when writing the script to the system, please try again later.");
                }

                if (ex is TimeoutException)
                {
                    if (!command.User.IsOwner()) command.User.IncrementExceptionLimit();

                    HandleResponse(command, null, new() { ErrorMessage = "script exceeded timeout", ExecutionTime = sw.Elapsed.TotalSeconds, Success = false });

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
                    global::Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                    isFailure = true;
                    Logger.Singleton.Warning(
                        "Failed to delete the user script '{0}' because '{1}'",
                        scriptName,
                        ex.Message
                    );
                }
            }
        }
        finally
        {
            sw.Stop();
            Logger.Singleton.Debug("Took {0}s to execute script slash command.", sw.Elapsed.TotalSeconds.ToString("f7"));

            if (isFailure)
            {
                _perfmon.TotalItemsProcessedThatFailed.Increment();
                _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                _perfmon.ExecuteScriptSlashCommandFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
            }
            else
            {
                _perfmon.ExecuteScriptSlashCommandSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
            }
        }
    }
}

#endif
