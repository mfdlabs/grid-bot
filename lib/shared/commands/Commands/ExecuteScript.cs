using Discord;
using Discord.WebSocket;

namespace Grid.Bot.Commands;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Logging;
using Drawing;
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

using HWND = System.IntPtr;

internal class ExecuteScript : IStateSpecificCommandHandler
{
    public string CommandName => "Execute Grid Server Lua Script";
    public string CommandDescription => $"Attempts to execute the given script contents on a grid " +
                                        $"server instance. Use xc, exc and executeconsole to execute with an image of the console";
    public string[] CommandAliases => new[] { "x", "ex", "execute", "xc", "exc", "executeconsole" };
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

    private const int MaxResultLength = EmbedBuilder.MaxDescriptionLength - 8;

    #region Metrics

    private static readonly ExecuteScriptCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

    #endregion Metrics

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

                var isAdminScript = global::Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

                var scriptId = NetworkingGlobal.GenerateUuidv4();
                var filesafeScriptId = scriptId.Replace("-", "");
                var scriptName = GridServerFileHelper.GetGridServerScriptPath(filesafeScriptId);

                // isAdmin allows a bypass of disabled methods and virtualized globals
                var (command, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
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
                    command
                );

                // bump to 20 seconds so it doesn't batch job timeout on first execution
                var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };

                var instance = GridServerArbiter.Singleton.GetOrCreateAvailableLeasedInstance();

                var wantsConsole = new[] { "xc", "exc", "executeconsole" }.Contains(originalCommand);

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
                                await message.ReplyWithFileAsync(
                                    screenshot,
                                    screenshotName
                                );

                            await message.ReplyWithFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(result)),
                                "execute-result.txt"
                            );
                            return;
                        }

                        var embed = new EmbedBuilder()
                                .WithTitle("Return value")
                                .WithDescription($"```\n{result}\n```")
                                .WithAuthor(message.Author)
                                .WithCurrentTimestamp()
                                .WithColor(0x00, 0xff, 0x00)
                                .Build();

                        if (wantsConsole)
                            await message.ReplyWithFileAsync(
                                screenshot,
                                screenshotName,
                                embed: embed
                            );
                        else
                            await message.ReplyAsync(
                                embed: embed
                            );

                        return;
                    }

                    if (wantsConsole)
                        await message.ReplyWithFileAsync(
                            screenshot,
                            screenshotName,
                            "Executed script with no return!"
                        );
                    else
                        await message.ReplyAsync(
                           "Executed script with no return!"
                        );

                }
                catch (Exception ex)
                {
                    isFailure = true;

                    if (ex is IOException)
                    {
                        global::Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                        await message.ReplyAsync("There was an IO error when writing the script to the system, please try again later.");
                    }

                    // We assume that it didn't actually track screenshots here.
                    instance.Lock();

                    var screenshot = GetScreenshotStream(instance);
                    var screenshotName = $"{instance.Name}.png";

                    if (ex is TimeoutException)
                    {
                        if (!message.Author.IsOwner()) message.Author.IncrementExceptionLimit();

                        if (wantsConsole)
                            await message.ReplyWithFileAsync(
                                screenshot,
                                screenshotName,
                                "The code you supplied executed for too long, please try again later."
                            );
                        else
                            await message.ReplyAsync(
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
                                await message.ReplyWithFileAsync(
                                    screenshot,
                                    screenshotName,
                                    "An error occured while executing your script:"
                                );
                                await message.ReplyWithFileAsync(
                                    new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                                    instance.Name + "txt"
                                );
                            }
                            else
                                await message.ReplyWithFileAsync(
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
                                .WithAuthor(message.Author)
                                .WithDescription($"```\n{fault.Message}\n```")
                                .Build();

                            if (wantsConsole)
                                await message.ReplyWithFileAsync(
                                    screenshot,
                                    screenshotName,
                                    "An error occured while executing your script:",
                                    embed: embed
                                );
                            else
                                await message.ReplyAsync(
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
