using Discord;
using Discord.WebSocket;

namespace Grid.Bot.Commands;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Loretta.CodeAnalysis;
using Loretta.CodeAnalysis.Lua;

using Logging;
using FileSystem;
using Networking;
using Diagnostics;
using ComputeCloud;
using Text.Extensions;
using Instrumentation;
using FloodCheckers.Core;
using FloodCheckers.Redis;

using Utility;
using Interfaces;
using Extensions;
using PerformanceMonitors;

using System.Collections.Generic;

internal class ExecuteScript : IStateSpecificCommandHandler
{
    public string CommandName => "Execute Grid Server Lua Script";
    public string CommandDescription => $"Attempts to execute the given script contents on a grid " +
                                        $"server instance.";
    public string[] CommandAliases => new[] { "x", "ex", "execute" };
    public bool Internal => false;
    public bool IsEnabled { get; set; } = true;
    private sealed class ExecuteScriptCommandPerformanceMonitor
    {
        private const string Category = "Grid.Commands.ExecuteScript";

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
        public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadAFileResultPerSecond { get; }
        public IAverageValueCounter ExecuteScriptCommandSuccessAverageTimeTicks { get; }
        public IAverageValueCounter ExecuteScriptCommandFailureAverageTimeTicks { get; }

        public ExecuteScriptCommandPerformanceMonitor(ICounterRegistry counterRegistry)
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
            TotalItemsProcessedThatHadAFileResultPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadAFileResultPerSecond", instance);
            ExecuteScriptCommandSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ExecuteScriptCommandSuccessAverageTimeTicks", instance);
            ExecuteScriptCommandFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ExecuteScriptCommandFailureAverageTimeTicks", instance);
        }
    }

    private const int MaxErrorLength = EmbedBuilder.MaxDescriptionLength - 8;
    private const int MaxResultLength = EmbedFieldBuilder.MaxFieldValueLength - 8;

    #region Metrics

    private static readonly ExecuteScriptCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

    #endregion Metrics

    private const string _floodCheckerCategory = "Grid.Commands.ExecuteScript.FloodChecking";

    private static readonly IFloodChecker _scriptExecutionFloodChecker = new RedisRollingWindowFloodChecker(
        _floodCheckerCategory,
        nameof(ExecuteScript),
        () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionFloodCheckerLimit,
        () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionFloodCheckerWindow,
        () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionFloodCheckingEnabled,
        Logger.Singleton,
        FloodCheckersRedisClientProvider.RedisClient
    );
    private static readonly ConcurrentDictionary<ulong, IFloodChecker> _perUserFloodCheckers = new();

    private static IFloodChecker GetPerUserFloodChecker(ulong userId)
    {
        return new RedisRollingWindowFloodChecker(
            _floodCheckerCategory,
            $"{nameof(ExecuteScript)}:{userId}",
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckerLimit,
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckerWindow,
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckingEnabled,
            Logger.Singleton,
            FloodCheckersRedisClientProvider.RedisClient
        );
    }

    private (string, MemoryStream) DetermineDescription(string input, string fileName)
    {
        if (input.IsNullOrEmpty()) return (null, null);

        if (input.Length > MaxResultLength)
        {
            if (input.Length / 1000 > global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxFileSizeKb)
                return ($"The result cannot be larger than {(global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxResultSizeKb)} KiB", null);

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

    public bool ParseLua(SocketMessage message, string input)
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
                .WithAuthor(message.Author)
                .WithCurrentTimestamp()
                .WithColor(0xff, 0x00, 0x00)
                .WithDescription($"```\n{errorString}\n```")
                .Build();

            message.Reply("There was a Luau syntax error in your script:", embed: embed);

            return false;
        }

        return true;
    }

    private void HandleResponse(SocketMessage message, string result, LuaUtility.ReturnMetadata metadata)
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
            message.ReplyWithFiles(
                attachments,
                text,
                embed: builder.Build()
            );
        else
            message.Reply(
                text,
                embed: builder.Build()
            );
    }

    public async Task Invoke(string[] contentArray, SocketMessage message, string originalCommand)
    {
        _perfmon.TotalItemsProcessed.Increment();
        var sw = Stopwatch.StartNew();
        bool isFailure = false;

        try
        {
            using (message.Channel.EnterTypingState())
            {
                var userIsAdmin = message.Author.IsAdmin();

                if (_scriptExecutionFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
                {
                    message.Reply("Too many people are using this command at once, please wait a few moments and try again.");
                    isFailure = true;
                    return;
                }

                _scriptExecutionFloodChecker.UpdateCount();

                var perUserFloodChecker = _perUserFloodCheckers.GetOrAdd(message.Author.Id, GetPerUserFloodChecker);
                if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
                {
                    message.Reply("You are sending script execution commands too quickly, please wait a few moments and try again.");
                    isFailure = true;
                    return;
                }

                perUserFloodChecker.UpdateCount();

                var script = contentArray.Join(" ");

                if (script.IsNullWhiteSpaceOrEmpty())
                {
                    _perfmon.TotalItemsProcessedThatHadEmptyScripts.Increment();
                    _perfmon.TotalItemsProcessedThatHadEmptyScriptsPerSecond.Increment();

                    // let's try and read the first attachment
                    if (message.Attachments.Count == 0)
                    {
                        _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment.Increment();
                        _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachmentPerSecond.Increment();

                        isFailure = true;
                        message.Reply("Script contents (up to 2000 chars, 4000 if nitro user), or 1 attachment was expected.");
                        return;
                    }

                    var firstAttachment = message.Attachments.First();
                    // TODO: Setting to disable this in case we want them to use any extension
                    //       because this message response can become ambigious

                    if (!firstAttachment.Filename.EndsWith(".lua"))
                    {
                        _perfmon.TotalItemsProcessedThatHadAnInvalidScriptFile.Increment();
                        _perfmon.TotalItemsProcessedThatHadAnInvalidScriptFilePerSecond.Increment();

                        isFailure = true;

                        message.Reply($"Expected the attachment ({firstAttachment.Filename}) to be a valid Lua file.");
                        return;
                    }

                    if (firstAttachment.Size / 1000 > global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxFileSizeKb)
                    {
                        isFailure = true;

                        message.Reply($"The input attachment ({firstAttachment.Filename}) cannot be larger than {(global::Grid.Bot.Properties.Settings.Default.ScriptExecutionMaxFileSizeKb)} KiB!");
                        return;
                    }

                    _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment.Increment();
                    _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachmentPerSecond.Increment();

                    script = firstAttachment.GetAttachmentContentsAscii();
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

                    await message.ReplyAsync($"The script you sent contains keywords that are not permitted, " +
                                             $"please review your script and change the blacklisted keyword: {keyword}");

                    return;
                }

                if (script.ContainsUnicode() && !global::Grid.Bot.Properties.Settings.Default.ScriptExecutionSupportUnicode && !userIsAdmin)
                {
                    _perfmon.TotalItemsProcessedThatHadUnicode.Increment();
                    _perfmon.TotalItemsProcessedThatHadUnicodePerSecond.Increment();

                    isFailure = true;

                    // TODO: Ack back the UTF-8 Characters if we can in the future.
                    await message.ReplyAsync("Sorry, but unicode in messages is not supported as of now, " +
                                             "please remove any unicode characters from your script.");
                    return;
                }

                if (!ParseLua(message, script))
                {
                    isFailure = true;
                    return;
                }

                var isAdminScript = global::Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

                var scriptId = NetworkingGlobal.GenerateUuidv4();
                var filesafeScriptId = scriptId.Replace("-", "");
                var scriptName = GridServerFileHelper.GetGridServerScriptPath(filesafeScriptId);

                // isAdmin allows a bypass of disabled methods and virtualized globals
                var (command, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
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
                    command
                );

                // bump to 20 seconds so it doesn't batch job timeout on first execution
                var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };

                try
                {
                    File.WriteAllText(scriptName, script, Encoding.ASCII);

                    var serverResult = GridServerArbiter.Singleton.BatchJobEx(job, scriptEx);
                    var (newResult, metadata) = LuaUtility.ParseResult(serverResult);

                    HandleResponse(message, newResult, metadata);

                }
                catch (Exception ex)
                {
                    isFailure = true;

                    if (ex is IOException)
                    {
                        global::Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                        await message.ReplyAsync("There was an IO error when writing the script to the system, please try again later.");
                    }

                    if (ex is TimeoutException)
                    {
                        if (!message.Author.IsOwner()) message.Author.IncrementExceptionLimit();

                        await message.ReplyAsync(
                            "The code you supplied executed for too long, please try again later."
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
        }
        finally
        {
            sw.Stop();
            Logger.Singleton.Debug("Took {0}s to execute script command.", sw.Elapsed.TotalSeconds.ToString("f7"));

            if (isFailure)
            {
                _perfmon.TotalItemsProcessedThatFailed.Increment();
                _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                _perfmon.ExecuteScriptCommandFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
            }
            else
            {
                _perfmon.ExecuteScriptCommandSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
            }
        }
    }
}
