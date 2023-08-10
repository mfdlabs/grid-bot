#if WE_LOVE_EM_SLASH_COMMANDS

using Discord;
using Discord.WebSocket;

namespace Grid.Bot.SlashCommands;


using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Logging;
using Drawing;
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

using HWND = System.IntPtr;
using System.ServiceModel.Channels;


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
            .AddOption("contents", ApplicationCommandOptionType.Attachment, "The Luau script attachment.", true)
            .AddOption("with_console", ApplicationCommandOptionType.Boolean, "Whether to include the console output.", false),

        new SlashCommandOptionBuilder()
            .WithName("text")
            .WithDescription("Execute a Luau script directly on the command line.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("contents", ApplicationCommandOptionType.String, "The Luau script contents.", true)
            .AddOption("with_console", ApplicationCommandOptionType.Boolean, "Whether to include the console output.", false)
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

    private const int MaxResultLength = EmbedBuilder.MaxDescriptionLength - 8;

    #region Metrics

    private static readonly ExecuteScriptSlashCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

    #endregion Metrics

    private const string _floodCheckerCategory = "Grid.SlashCommands.ExecuteScript.FloodChecking";

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
            $"{nameof(ScriptExecution)}:{userId}",
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckerLimit,
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckerWindow,
            () => global::Grid.Bot.Properties.Settings.Default.ScriptExecutionPerUserFloodCheckingEnabled,
            Logger.Singleton,
            FloodCheckersRedisClientProvider.RedisClient
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

    private static void MaximizeGridServer([In] HWND hWnd)
    {
        const int SW_MAXIMIZE = 3;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(HWND hWnd, int nCmdShow);

        ShowWindow(hWnd, SW_MAXIMIZE);
    }

    private static Stream GetScreenshotStream(ILeasedGridServerInstance inst)
    {
        var tempFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", $"{inst.Name}");
        try
        {
            var mainWindowHandle = Process.GetProcessById(inst.Process.Process.Id).MainWindowHandle;
            MaximizeGridServer(mainWindowHandle);
            var bitMap = mainWindowHandle.GetBitmapForWindowByWindowHandle();
            bitMap.Save(tempFileName);
            return new MemoryStream(File.ReadAllBytes(tempFileName));
        }
        finally
        {
            tempFileName.PollDeletion();
        }
    }

    public async Task Invoke(SocketSlashCommand command)
    {
        _perfmon.TotalItemsProcessed.Increment();

        var sw = Stopwatch.StartNew();
        var isFailure = false;

        try
        {
            var userIsAdmin = command.User.IsAdmin();

            if (_scriptExecutionFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
            {
                await command.RespondEphemeralAsync("Too many people are using this command at once, please wait a few moments and try again.");
                isFailure = true;
                return;
            }

            _scriptExecutionFloodChecker.UpdateCount();

            var perUserFloodChecker = _perUserFloodCheckers.GetOrAdd(command.User.Id, GetPerUserFloodChecker);
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

            var isAdminScript = global::Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

            var scriptId = NetworkingGlobal.GenerateUuidv4();
            var filesafeScriptId = scriptId.Replace("-", "");
            var scriptName = GridServerFileHelper.GetGridServerScriptPath(filesafeScriptId);

            // isAdmin allows a bypass of disabled methods and virtualized globals
            var (gserverCommand, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
                filesafeScriptId,
                ("isAdmin", isAdminScript),
                ("isVmEnabledForAdmins", global::Grid.Bot.Properties.Settings.Default.ShouldAdminsUseVM)
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

            var instance = GridServerArbiter.Singleton.GetOrCreateAvailableLeasedInstance();
            var expirationTime = new DateTimeOffset(instance.Expiration).ToUnixTimeSeconds();

            var wantsConsole = subcommand.GetOptionValue("with_console")?.ToString()?.ToLower() == "true";

            try
            {
                File.WriteAllText(scriptName, script, Encoding.ASCII);

                var result = LuaUtility.ParseLuaValues(instance.BatchJobEx(job, scriptEx));

                instance.Lock();

                var screenshot = GetScreenshotStream(instance);
                var screenshotName = $"{instance.Name}.png";

                if (!result.IsNullOrEmpty())
                {
                    if (result.Length > MaxResultLength)
                    {
                        _perfmon.TotalItemsProcessedThatHadAFileResult.Increment();

                        if (wantsConsole)
                            await command.RespondWithFilePublicAsync(
                                screenshot,
                                screenshotName
                            );

                        await command.RespondWithFilePublicAsync(new MemoryStream(Encoding.UTF8.GetBytes(result)),
                            "execute-result.txt"
                        );
                        return;
                    }

                    var embed = new EmbedBuilder()
                            .WithTitle("Return value")
                            .WithDescription($"```\n{result}\n```")
                            .WithAuthor(command.User)
                            .WithCurrentTimestamp()
                            .WithColor(0x00, 0xff, 0x00)
                            .Build();

                    if (wantsConsole)
                        await command.RespondWithFilePublicAsync(
                            screenshot,
                            screenshotName,
                            embed: embed
                        );
                    else
                        await command.RespondPublicAsync(
                            embed: embed
                        );

                    return;
                }

                if (wantsConsole)
                    await command.RespondWithFilePublicAsync(
                        screenshot,
                        screenshotName,
                        "Executed script with no return!"
                    );
                else
                    await command.RespondPublicAsync(
                       "Executed script with no return!"
                    );
            }
            catch (Exception ex)
            {
                isFailure = true;

                if (ex is IOException)
                {
                    global::Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                    await command.RespondEphemeralPingAsync("There was an IO error when writing the script to the system, please try again later.");
                }

                // We assume that it didn't actually track screenshots here.
                instance.Lock();
                var screenshot = GetScreenshotStream(instance);
                var screenshotName = $"{instance.Name}.png";

                if (ex is TimeoutException)
                {
                    if (!command.User.IsOwner()) command.User.IncrementExceptionLimit();

                    if (wantsConsole)
                        await command.RespondWithFilePublicAsync(
                            screenshot,
                            screenshotName,
                            "The code you supplied executed for too long, please try again later."
                        );
                    else
                        await command.RespondPublicAsync(
                            "The code you supplied executed for too long, please try again later."
                        );

                    return;
                }

                if (ex is FaultException fault)
                {
                    if (fault.Message.Length + 8 > EmbedBuilder.MaxDescriptionLength)
                    {
                        // Respond with file instead
                        if (wantsConsole)
                        {
                            await command.RespondWithFilePublicAsync(
                                screenshot,
                                screenshotName,
                                "An error occured while executing your script:"
                            );
                            await command.RespondWithFilePublicAsync(
                                new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                                instance.Name + "txt"
                            );
                        }
                        else
                            await command.RespondWithFilePublicAsync(
                                new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                                instance.Name + "txt",
                                "An error occured while executing your script:"
                            );
                    }
                    else
                    {
                        var embed = new EmbedBuilder()
                            .WithColor(0xff, 0x00, 0x00)
                            .WithTitle("Luau Error")
                            .WithAuthor(command.User)
                            .WithDescription($"```\n{fault.Message}\n```")
                            .Build();

                        if (wantsConsole)
                            await command.RespondWithFilePublicAsync(
                                screenshot,
                                screenshotName,
                                "An error occured while executing your script:",
                                embed: embed
                            );
                        else
                            await command.RespondPublicAsync(
                                "An error occured while executing your script:",
                                embed: embed
                            );
                    }

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

                try
                {
                    if (instance != null)
                    {
                        instance.Unlock();
                        instance.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    global::Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                    isFailure = true;

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
