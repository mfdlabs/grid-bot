/*
File Name: ScriptExecutionQueueUserMetricsTask.cs
Description: A Concurrent task used to process script execution requests in a queue.
Written By: Nikita Petko, Alex Bkordan, Jakob Valara

TODO: Fix something about this.
*/


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Discord;
using MFDLabs.Concurrency;
using MFDLabs.Concurrency.Base;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.Bot.Plugins;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Networking;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot
{
    namespace Tasks
    {
        internal sealed class ScriptExecutionQueueUserMetricsTask : ExpiringTaskThread<ScriptExecutionQueueUserMetricsTask, SocketTaskRequest>
        {
            protected override string Name => "Script Execution Queue";
            protected override TimeSpan ProcessActivationInterval => global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionQueueDelay;
            protected override TimeSpan Expiration => global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionQueueExpiration;
            protected override ICounterRegistry CounterRegistry => PerfmonCounterRegistryProvider.Registry;
            protected override int PacketId => 17;

            public override PluginResult OnReceive(ref Packet<SocketTaskRequest> packet)
            {
                var message = packet.Item.Message;
                var perfmon = GetUserPerformanceMonitor(message.Author);

                try
                {
                    perfmon.TotalItemsProcessed.Increment();
                    var result = ScriptExecutionTaskPlugin.Singleton.OnReceive(ref packet);
                    perfmon.TotalItemsProcessedThatSucceeded.Increment();
                    return result;
                }
                catch (Exception ex)
                {
                    message.Author.FireEvent("RenderQueueFailure", ex.ToDetailedString());
                    perfmon.TotalItemsProcessedThatFailed.Increment();
                    packet.Status = PacketProcessingStatus.Failure;

                    if (ex is FaultException fault)
                    {
                        SystemLogger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                        switch (fault.Message)
                        {
                            case "Cannot invoke BatchJob while another job is running":
                                message.Reply("You are sending requests too fast, please slow down!");
                                return PluginResult.ContinueProcessing;
                            case "BatchJob Timeout":
                                message.Reply("The job timed out, please try again later.");
                                return PluginResult.ContinueProcessing;
                        }

                        if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                        {
                            message.ReplyWithFile(new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                                "fault.txt",
                                "An exception occurred on the grid server, please review this error to see if your input was malformed:");
                            return PluginResult.ContinueProcessing;
                        }

                        message.Reply(
                            "An exception occurred on the grid server, please review this error to see if " +
                            "your input was malformed:",
                            embed: new EmbedBuilder()
                            .WithColor(0xff, 0x00, 0x00)
                            .WithTitle("GridServer exception.")
                            .WithAuthor(message.Author)
                            .WithDescription($"```\n{fault.Message}\n```")
                            .Build()
                        );
                        return PluginResult.ContinueProcessing;
                    }

#if DEBUG
                    SystemLogger.Singleton.Error(ex);
#else
                    SystemLogger.Singleton.Warning("An error occurred when trying to execute render task: {0}", ex.Message);
#endif
                    if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
                    {
                        var detail = ex.ToDetailedString();
                        if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                        {
                            message.ReplyWithFile(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt");
                            return PluginResult.ContinueProcessing;
                        }

                        message.Reply(
                            "An error occured with the script execution task and the environment variable" +
                            "'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                            embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                        );
                        return PluginResult.ContinueProcessing;
                    }
                    message.Reply("An error occurred when trying to execute script task, please try again later.");
                    return PluginResult.ContinueProcessing;
                }
            }

            private UserTaskPerformanceMonitor GetUserPerformanceMonitor(IUser user)
            {
                var perfmon = (from userPerfmon in _userPerformanceMonitors
                    where userPerfmon.Item1 == user.Id
                    select userPerfmon.Item2).FirstOrDefault();

                if (perfmon == default)
                {
                    perfmon = new UserTaskPerformanceMonitor(CounterRegistry, "RenderTask", user);
                    _userPerformanceMonitors.Add((user.Id, perfmon));
                }

                return perfmon;
            }

            private readonly ICollection<(ulong, UserTaskPerformanceMonitor)> _userPerformanceMonitors = new List<(ulong, UserTaskPerformanceMonitor)>();
        }
    }

    namespace Plugins
    {
        internal sealed class ScriptExecutionTaskPlugin : BasePlugin<ScriptExecutionTaskPlugin, SocketTaskRequest>
        {

            private const int MaxResultLength = EmbedBuilder.MaxDescriptionLength - 8;

            #region Concurrency

            private readonly object _execLock = new();
            private bool _processingItem;
            private int _itemCount;

            #endregion Concurrency

            #region Metrics

            private readonly ScriptExecutionTaskPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

            #endregion Metrics

            public override PluginResult OnReceive(ref Packet<SocketTaskRequest> packet)
            {
                var metrics = PacketMetricsPlugin<SocketTaskRequest>.Singleton.OnReceive(ref packet);

                if (metrics == PluginResult.StopProcessingAndDeallocate) return PluginResult.StopProcessingAndDeallocate;

                if (packet.Item != null)
                {
                    var message = packet.Item.Message;
                    var contentArray = packet.Item.ContentArray;

                    _perfmon.TotalItemsProcessed.Increment();
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        _itemCount++;
                        using (message.Channel.EnterTypingState())
                        {

                            if (_processingItem)
                            {
                                message.Reply($"The script execution queue is currently trying to process " +
                                              $"{_itemCount} items, please wait for your result to be processed.");
                            }

                            lock (_execLock)
                            {
                                _processingItem = true;

                                var userIsAdmin = message.Author.IsAdmin();

                                var script = contentArray.Join(" ");



                                if (script.IsNullWhiteSpaceOrEmpty())
                                {
                                    _perfmon.TotalItemsProcessedThatHadEmptyScripts.Increment();

                                    // let's try and read the attachments.
                                    if (message.Attachments.Count == 0)
                                    {
                                        _perfmon.TotalItemsProcessedThatFailed.Increment();
                                        _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnNoAttachment.Increment();
                                        message.Reply("The script or at least 1 attachment was expected.");
                                        return PluginResult.ContinueProcessing;
                                    }

                                    var firstAttachment = message.Attachments.First();
                                    if (!firstAttachment.Filename.EndsWith(".lua"))
                                    {
                                        _perfmon.TotalItemsProcessedThatFailed.Increment();
                                        _perfmon.TotalItemsProcessedThatHadAnInvalidScriptFile.Increment();
                                        message.Reply("The attachment is required to be a valid Lua file.");
                                        return PluginResult.ContinueProcessing;
                                    }

                                    _perfmon.TotalItemsProcessedThatHadEmptyScriptsButHadAnAttachment.Increment();

                                    script = firstAttachment.GetAttachmentContentsAscii();
                                }

                                // remove phone specific quotes (why would you want them anyway? they are unicode)
                                script = script.EscapeQuotes();
                                script = script.GetCodeBlockContents();

                                if (LuaUtility.CheckIfScriptContainsDisallowedText(script, out var keyword) && !userIsAdmin)
                                {
                                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                                    _perfmon.TotalItemsProcessedThatHadBlacklistedKeywords.Increment();
                                    message.Reply($"The script you sent contains keywords that are not permitted, " +
                                                  $"please review your script and change the blacklisted keyword: {keyword}");
                                    return PluginResult.ContinueProcessing;
                                }

                                if (script.ContainsUnicode() && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionSupportUnicode && !userIsAdmin)
                                {
                                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                                    _perfmon.TotalItemsProcessedThatHadUnicode.Increment();
                                    message.Reply("Sorry, but unicode in messages is not supported as of now, " +
                                                  "please remove any unicode characters from your script.");
                                    return PluginResult.ContinueProcessing;
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

                                if (isAdminScript) SystemLogger.Singleton.Debug("Admin scripts are enabled, disabling VM.");

                                try
                                {
                                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionRequireProtections)
                                        script = $"{LuaUtility.SafeLuaMode}{script}";

                                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionPrependBaseURL)
                                        script = $"game:GetService(\"ContentProvider\"):SetBaseUrl" +
                                                 $"(\"{MFDLabs.Grid.Bot.Properties.Settings.Default.BaseURL}\");{script}";

                                    File.WriteAllText(scriptName, script, Encoding.ASCII);

                                    var scriptEx = Lua.NewScript(
                                        NetworkingGlobal.GenerateUuidv4(),
                                        command
                                    );

                                    // bump to 20 seconds so it doesn't batch job timeout on first execution
                                    var job = new Job() { id = scriptId, expirationInSeconds = userIsAdmin ? 20000 : 20 };
                                    var result = LuaUtility.ParseLuaValues(GridServerArbiter.Singleton.BatchJobEx(job, scriptEx));

                                    if (!result.IsNullOrEmpty())
                                    {
                                        if (result.Length > MaxResultLength)
                                        {
                                            _perfmon.TotalItemsProcessedThatHadAFileResult.Increment();
                                            message.ReplyWithFile(new MemoryStream(Encoding.UTF8.GetBytes(result)),
                                                "execute-result.txt",
                                                "Executed script with return:");
                                            return PluginResult.ContinueProcessing;
                                        }
                                        message.Reply(
                                            "Executed script with return:",
                                            embed: new EmbedBuilder()
                                            .WithTitle("Return value")
                                            .WithDescription($"```\n{result}\n```")
                                            .WithAuthor(message.Author)
                                            .WithCurrentTimestamp()
                                            .WithColor(0x00, 0xff, 0x00)
                                            .Build()
                                        );

                                        return PluginResult.ContinueProcessing;
                                    }
                                    message.Reply("Executed script with no return!");

                                }
                                catch (IOException)
                                {
                                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                                    message.Reply("There was an IO error when writing the script to the system, " +
                                                  "please try again later.");
                                }
                                finally
                                {
                                    try
                                    {
                                        SystemLogger.Singleton.LifecycleEvent(
                                            "Trying delete the script '{0}' at path '{1}'",
                                            scriptId,
                                            scriptName);
                                        File.Delete(scriptName);
                                        SystemLogger.Singleton.LifecycleEvent(
                                            "Successfully deleted the script '{0}' at path '{1}'!",
                                            scriptId,
                                            scriptName);
                                    }
                                    catch (Exception ex)
                                    {
                                        _perfmon.TotalItemsProcessedThatFailed.Increment();
                                        SystemLogger.Singleton.Warning(
                                            "Failed to delete the user script '{0}' because '{1}'",
                                            scriptName,
                                            ex.Message);
                                    }
                                }
                            }
                        }

                        return PluginResult.ContinueProcessing;
                    }
                    finally
                    {
                        _itemCount--;
                        _processingItem = false;
                        sw.Stop();
                        SystemLogger.Singleton.Debug("Took {0}s to execute render task.",
                            sw.Elapsed.TotalSeconds.ToString("f7"));
                    }
                }
                else
                {
                    SystemLogger.Singleton.Warning("Task packet {0} at the sequence {1} had a null item, ignoring...",
                        packet.Id,
                        packet.SequenceId);
                    return global::MFDLabs.Grid.Bot.Properties.Settings.Default.StopProcessingOnNullPacketItem
                        ? PluginResult.StopProcessingAndDeallocate
                        : PluginResult.ContinueProcessing;
                }
            }
        }
    }
}