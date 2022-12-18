﻿#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.FileSystem;
using MFDLabs.Networking;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal class ExecuteScript : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Execute Luau Script";
        public string CommandAlias => "execute";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;

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
            private const string Category = "MFDLabs.Grid.SlashCommands.ExecuteScript";

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

        public async Task Invoke(SocketSlashCommand command)
        {
            _perfmon.TotalItemsProcessed.Increment();

            var sw = Stopwatch.StartNew();
            var isFailure = false;

            try
            {
                var userIsAdmin = command.User.IsAdmin();

                if (command.HasReachedMaximumExecutionCount(out var nextAvailableExecutionDate) && !userIsAdmin)
                {
                    await command.RespondEphemeralPingAsync($"You have reached the maximum script execution count of 25, you may execute again after <t:{new DateTimeOffset(nextAvailableExecutionDate ?? DateTime.UtcNow).ToUnixTimeSeconds()}:T>.");
                    return;
                }

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

                if (script.ContainsUnicode() && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExecutionSupportUnicode && !userIsAdmin)
                {
                    _perfmon.TotalItemsProcessedThatHadUnicode.Increment();
                    _perfmon.TotalItemsProcessedThatHadUnicodePerSecond.Increment();

                    isFailure = true;

                    // TODO: Ack back the UTF-8 Characters if we can in the future.
                    await command.RespondEphemeralPingAsync("Sorry, but unicode in messages is not supported as of now, " +
                                                            "please remove any unicode characters from your script.");
                    return;
                }

                var isAdminScript = global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminScripts && userIsAdmin;

                var scriptId = NetworkingGlobal.GenerateUuidv4();
                var filesafeScriptId = scriptId.Replace("-", "");
                var scriptName = GridServerCommandUtility.GetGridServerScriptPath(filesafeScriptId);

                // isAdmin allows a bypass of disabled methods and virtualized globals
                var (gserverCommand, _) = JsonScriptingUtility.GetSharedGameServerExecutionScript(
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
                    gserverCommand
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

                    command.CreateGridServerInstanceReference(ref instance);



                    if (!result.IsNullOrEmpty())
                    {
                        if (result.Length > MaxResultLength)
                        {
                            _perfmon.TotalItemsProcessedThatHadAFileResult.Increment();
                            await command.RespondWithFilePublicPingAsync(new MemoryStream(Encoding.UTF8.GetBytes(result)),
                                "execute-result.txt",
                                $"This instance will expire at <t:{expirationTime}:T>"
                            );
                            return;
                        }

                        await command.RespondPublicPingAsync(
                            $"This instance will expire at <t:{expirationTime}:T>",
                            embed: new EmbedBuilder()
                                .WithTitle("Return value")
                                .WithDescription($"```\n{result}\n```")
                                .WithAuthor(command.User)
                                .WithCurrentTimestamp()
                                .WithColor(0x00, 0xff, 0x00)
                                .Build()
                        );

                        return;
                    }

                    await command.RespondPublicPingAsync($"Executed script with no return! This instance will expire at <t:{expirationTime}:T>");


                }
                catch (Exception ex)
                {
                    isFailure = true;

                    if (ex is IOException)
                    {
                        global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);
                        await command.RespondEphemeralPingAsync("There was an IO error when writing the script to the system, please try again later.");
                    }

                    // We assume that it didn't actually track screenshots here.
                    instance.Lock();
                    command.CreateGridServerInstanceReference(ref instance);

                    await command.RespondPublicPingAsync($"This instance will expire at <t:{expirationTime}:T>");


                    if (ex is TimeoutException)
                    {
                        if (!command.User.IsOwner()) command.User.IncrementExceptionLimit();

                        await command.RespondPublicPingAsync("The code you supplied executed for too long, please try again later.");

                        return;
                    }

                    if (ex is not IOException) throw;
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
}

#endif