using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ServiceModel;
using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Microsoft.Ccr.Core;
using MFDLabs.Logging;
using MFDLabs.FileSystem;
using MFDLabs.Networking;
using MFDLabs.Concurrency;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Instrumentation;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;

namespace MFDLabs.Grid.Bot.WorkQueues
{
    public sealed class ScriptExecutionWorkQueue : AsyncWorkQueue<SocketTaskRequest>
    {
        private sealed class ScriptExecutionWorkQueuePerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.WorkQueues.ScriptExecutionWorkQueue";

            public IRawValueCounter TotalItemsProcessed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
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
            public IAverageValueCounter ScriptExecutionWorkQueueSuccessAverageTimeTicks { get; }
            public IAverageValueCounter ScriptExecutionWorkQueueFailureAverageTimeTicks { get; }

            public ScriptExecutionWorkQueuePerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
                TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedPerSecond", instance);
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
                ScriptExecutionWorkQueueSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ScriptExecutionWorkQueueSuccessAverageTimeTicks", instance);
                ScriptExecutionWorkQueueFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ScriptExecutionWorkQueueFailureAverageTimeTicks", instance);
            }
        }

        private const string OnCareToLeakException = "An error occured with the script execution work queue task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:";
        private const string JobAlreadyRunningException = "Cannot invoke BatchJob while another job is running";
        private const string BatchJobTimeout = "BatchJob Timeout";
        private const int MaxResultLength = EmbedBuilder.MaxDescriptionLength - 8;

        private ScriptExecutionWorkQueue()
            : base(WorkQueueDispatcherQueueRegistry.ScriptExecutionQueue, OnReceive)
        { }

        // Doesn't break HATE SINGLETON because we never need multiple instances of this
        public static readonly ScriptExecutionWorkQueue Singleton = new();

        private static readonly ConcurrentDictionary<ulong, UserWorkQueuePerformanceMonitor> _userPerformanceMonitors = new();
        private static UserWorkQueuePerformanceMonitor GetUserPerformanceMonitor(IUser user)
            => _userPerformanceMonitors.GetOrAdd(user.Id, _ => new UserWorkQueuePerformanceMonitor(PerfmonCounterRegistryProvider.Registry, "ScriptExecutionWorkQueue", user));

        private static void HandleWorkQueueException(Exception ex, SocketMessage message, UserWorkQueuePerformanceMonitor perf)
        {
            if (ex is not FaultException or TimeoutException)
                global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);

            message.Author.FireEvent("ScriptExecutionWorkQueueFailure", ex.ToDetailedString());

            perf.TotalItemsProcessedThatFailed.Increment();
            perf.TotalItemsProcessedThatFailedPerSecond.Increment();

            switch (ex)
            {
                case TimeoutException:
                    message.Reply("The code you supplied executed for too long, please try again later.");
                    message.Author.IncrementExceptionLimit();
                    return;
                case FaultException fault:
                {
                    Logger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                    switch (fault.Message)
                    {
                        case JobAlreadyRunningException:
                            message.Reply("You are sending execution requests too fast, please slow down!");
                            return;
                        case BatchJobTimeout:
                            message.Reply("The code you supplied executed for too long, please try again later.");
                            message.Author.IncrementExceptionLimit();
                            return;
                    }

                    if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                    {
                        message.ReplyWithFile(new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                            "fault.txt",
                            "An exception occurred on the grid server, please review this error to see if your input was malformed:");
                        return;
                    }

                    message.Reply(
                        "An exception occurred on the grid server, please review this error to see if " +
                        "your input was malformed:",
                        embed: new EmbedBuilder()
                            .WithColor(0xff, 0x00, 0x00)
                            .WithTitle("Grid Server exception.")
                            .WithAuthor(message.Author)
                            .WithDescription($"```\n{fault.Message}\n```")
                            .Build()
                    );

                    return;
                }
            }

#if DEBUG || DEBUG_LOGGING_IN_PROD
            Logger.Singleton.Error(ex);
#else
            Logger.Singleton.Warning("An error occurred when trying to execute render work queue task: {0}", ex.Message);
#endif

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    message.ReplyWithFile(
                        new MemoryStream(Encoding.UTF8.GetBytes(detail)),
                        "script-execution-work-queue-ex.txt",
                        OnCareToLeakException
                    );
                    return;
                }

                message.Reply(
                    OnCareToLeakException,
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }
            message.Reply("An error occurred when trying to execute script execution work queue task, please try again later.");
        }

        private static void OnReceive(SocketTaskRequest item, SuccessFailurePort result)
        {
            var message = item.Message;
            var perfmon = GetUserPerformanceMonitor(message.Author);

            try
            {
                perfmon.TotalItemsProcessed.Increment();
                perfmon.TotalItemsProcessedPerSecond.Increment();
                ProcessItem(item);
                perfmon.TotalItemsProcessedThatSucceeded.Increment();
                perfmon.TotalItemsProcessedThatSucceededPerSecond.Increment();
                result.Post(SuccessResult.Instance);
            }
            catch (Exception ex) { result.Post(ex); HandleWorkQueueException(ex, message, perfmon); }
        }

        #region Metrics

        private static readonly ScriptExecutionWorkQueuePerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

        private static void ProcessItem(SocketTaskRequest item)
        {
            if (item == null) throw new ApplicationException("Got Null Item reference.");

            var message = item.Message;
            var contentArray = item.ContentArray;

            _perfmon.TotalItemsProcessed.Increment();
            var sw = Stopwatch.StartNew();
            bool isFailure = false;

            try
            {
                using (message.Channel.EnterTypingState())
                {
                    var userIsAdmin = message.Author.IsAdmin();
                    
                    if (message.HasReachedMaximumExecutionCount(out var nextAvailableExecutionDate) && !userIsAdmin)
                    {
                        message.Reply($"You have reached the maximum script execution count of 25, you may execute again after <t:{new DateTimeOffset(nextAvailableExecutionDate.Value ?? DateTime.UtcNow).ToUnixTimeSeconds()}:T>.");
                        return;
                    }
                    
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

                        message.Reply($"The script you sent contains keywords that are not permitted, " +
                                      $"please review your script and change the blacklisted keyword: {keyword}");

                        return;
                    }

                    if (script.ContainsUnicode() && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionSupportUnicode && !userIsAdmin)
                    {
                        _perfmon.TotalItemsProcessedThatHadUnicode.Increment();
                        _perfmon.TotalItemsProcessedThatHadUnicodePerSecond.Increment();

                        isFailure = true;

                        // TODO: Ack back the UTF-8 Characters if we can in the future.
                        message.Reply("Sorry, but unicode in messages is not supported as of now, " +
                                      "please remove any unicode characters from your script.");
                        return;
                    }

                    var isAdminScript = global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

                    var scriptId = NetworkingGlobal.GenerateUuidv4();
                    var filesafeScriptId = scriptId.Replace("-", "");
                    var scriptName = GridServerCommandUtility.GetGridServerScriptPath(filesafeScriptId);

                    // isAdmin allows a bypass of disabled methods and virtualized globals
                    var (command, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
                        filesafeScriptId,
                        ("isAdmin", isAdminScript)
                    );

                    if (isAdminScript) Logger.Singleton.Debug("Admin scripts are enabled, disabling VM.");

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionRequireProtections)
                        script = $"{LuaUtility.SafeLuaMode}{script}";

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionPrependBaseURL)
                        script = $"game:GetService(\"ContentProvider\"):SetBaseUrl" +
                                 $"(\"{MFDLabs.Grid.Bot.Properties.Settings.Default.BaseURL}\");{script}";

                    var scriptEx = Lua.NewScript(
                        NetworkingGlobal.GenerateUuidv4(),
                        command
                    );

                    // bump to 20 seconds so it doesn't batch job timeout on first execution
                    var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };

                    var instance = GridServerArbiter.Singleton.GetOrCreateAvailableLeasedInstance();
                    var expirationTime = new DateTimeOffset(instance.Expiration).ToUnixTimeSeconds();

                    try
                    {
                        File.WriteAllText(scriptName, script, Encoding.ASCII);

                        var result = LuaUtility.ParseLuaValues(instance.BatchJobEx(job, scriptEx));

                        instance.Lock();

                        message.CreateGridServerInstanceReference(ref instance);



                        if (!result.IsNullOrEmpty())
                        {
                            if (result.Length > MaxResultLength)
                            {
                                _perfmon.TotalItemsProcessedThatHadAFileResult.Increment();
                                message.ReplyWithFile(new MemoryStream(Encoding.UTF8.GetBytes(result)), 
                                    "execute-result.txt",
                                $"This instance will expire at <t:{expirationTime}:T>"
                                );
                                return;
                            }
                            message.Reply(
                                $"This instance will expire at <t:{expirationTime}:T>",
                                embed: new EmbedBuilder()
                                .WithTitle("Return value")
                                .WithDescription($"```\n{result}\n```")
                                .WithAuthor(message.Author)
                                .WithCurrentTimestamp()
                                .WithColor(0x00, 0xff, 0x00)
                                .Build()
                            );

                            return;
                        }

                        message.Reply($"Executed script with no return! This instance will expire at <t:{expirationTime}:T>");


                    }
                    catch (Exception ex)
                    {
                        isFailure = true;

                        if (ex is IOException)
                        {
                            global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                            message.Reply("There was an IO error when writing the script to the system, please try again later.");
                        }

                        // We assume that it didn't actually track screenshots here.
                        instance.Lock();
                        message.CreateGridServerInstanceReference(ref instance);

                        message.Reply($"This instance will expire at <t:{expirationTime}:T>");


                        if (ex is not IOException) throw; // rethrow.
                    }
                    finally
                    {
                        try
                        {
                            Logger.Singleton.LifecycleEvent(
                                "Trying delete the script '{0}' at path '{1}'",
                                scriptId,
                                scriptName
                            );
                            scriptName.PollDeletion(
                                10,
                                ex => Logger.Singleton.Warning("Failed to delete '{0}' because: {1}", scriptName, ex.Message),
                                () => Logger.Singleton.LifecycleEvent(
                                    "Successfully deleted the script '{0}' at path '{1}'!",
                                        scriptId,
                                        scriptName
                                    )
                            );
                        }
                        catch (Exception ex)
                        {
                            global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);
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
                Logger.Singleton.Debug("Took {0}s to execute script execution work queue task.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (isFailure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.ScriptExecutionWorkQueueFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
                else
                {
                    _perfmon.ScriptExecutionWorkQueueSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }
    }
}
