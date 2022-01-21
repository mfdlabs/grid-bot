/*
    Name: CommandRegistry.cs
    Written By: Alex Bkordan
    Description: C# Runtime parser for a command registry
*/

// Jakob: TODO, Load these commands from a different assembly so they can be changed at runtime?
//              It will have to load the assembly, and this shouldn't have a reference to it.
// Alex: That won't really work if we want to keep up shared settings.
// Nikita: Shut up, I did it anyway, you guys slow and suck at code


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Guards;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Logging;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Sentinels;
using MFDLabs.Text.Extensions;

#if WE_LOVE_EM_SLASH_COMMANDS
using MFDLabs.Grid.Bot.Global;
// ReSharper disable AsyncVoidLambda
#endif // WE_LOVE_EM_SLASH_COMMANDS

namespace MFDLabs.Grid.Bot.Registries
{
    public static class CommandRegistry
    {
        private static bool _wasRegistered;

        private static readonly object RegistrationLock = new();

        private static readonly ICollection<CommandCircuitBreakerWrapper> CircuitBreakerWrappers = new List<CommandCircuitBreakerWrapper>();
        private static readonly ICollection<IStateSpecificCommandHandler> StateSpecificCommandHandlers = new List<IStateSpecificCommandHandler>();
        private static ICollection<(IStateSpecificCommandHandler, string)> _disabledCommandsReasons = new List<(IStateSpecificCommandHandler, string)>();

#if WE_LOVE_EM_SLASH_COMMANDS

        private static readonly ICollection<IStateSpecificSlashCommandHandler> StateSpecificSlashCommandHandlers = new List<IStateSpecificSlashCommandHandler>();
        private static ICollection<(IStateSpecificSlashCommandHandler, string)> _disabledSlashCommandsReasons = new List<(IStateSpecificSlashCommandHandler, string)>();

#endif // WE_LOVE_EM_SLASH_COMMANDS

        private static readonly CommandRegistryInstrumentationPerformanceMonitor InstrumentationPerfmon =
            new(PerfmonCounterRegistryProvider.Registry);

        private static string GetDefaultCommandNamespace() => "MFDLabs.Grid.Bot.Commands";

#if WE_LOVE_EM_SLASH_COMMANDS

        private static string GetSlashCommandNamespace() => "MFDLabs.Grid.Bot.SlashCommands";

#endif // WE_LOVE_EM_SLASH_COMMANDS

        public static Embed ConstructHelpEmbedForSingleCommand(string commandName, IUser author)
        {
            if (!_wasRegistered) RegisterOnce();

            var command = GetCommandByCommandAlias(commandName);

            if (command == default) return null;

            var isInternal = command.Internal;
            var isDisabled = command.IsEnabled == false;

            if (isInternal && !author.IsAdmin()) return null;

            var builder = new EmbedBuilder
            {
                Title = $"{command.CommandName} Documentation",
                Color = isDisabled ? new Color(0xff, 0x00, 0x00) : new Color(0x00, 0xff, 0x00)
            };
            builder.AddField(string.Join(", ",
                    command.CommandAliases),
                $"{command.CommandDescription}\n{(isInternal ? ":no_entry:" : "")} {(isDisabled ? ":x:" : ":white_check_mark:")}");
            builder.WithCurrentTimestamp();
            builder.Description = ":no_entry:\\: **INTERNAL**\n:x:\\: **DISABLED**\n:white_check_mark:\\: **ENABLED**";

            return builder.Build();
        }

        public static ICollection<Embed> ConstructHelpEmbedForAllCommands(IUser author)
        {
            if (!_wasRegistered) RegisterOnce();

            var builder = new EmbedBuilder().WithTitle("Documentation");
            var embeds = new List<Embed>();
            var i = 0;

            foreach (var command in StateSpecificCommandHandlers)
            {
                if (i == 24)
                {
                    embeds.Add(builder.Build());
                    builder = new EmbedBuilder();
                    i = 0;
                }

                var isInternal = command.Internal;
                var isDisabled = command.IsEnabled == false;
                if (isInternal && !author.IsAdmin()) continue;

                builder.AddField(
                    $"{command.CommandName}: {string.Join(", ", command.CommandAliases)}",
                    $"{command.CommandDescription}\n{(isInternal ? ":no_entry:" : "")} {(isDisabled ? ":x:" : ":white_check_mark:")}"
                );

                builder.Color = new Color(0x00, 0x99, 0xff);
                builder.WithCurrentTimestamp();
                i++;
            }

            if (i < 24) embeds.Add(builder.Build());
            builder.Color = new Color(0x00, 0x99, 0xff);
            builder = new EmbedBuilder();
            builder.WithCurrentTimestamp();
            builder.Description = ":no_entry:\\: **INTERNAL**\n:x:\\: **DISABLED**\n:white_check_mark:\\: **ENABLED**";
            embeds.Add(builder.Build());

            return embeds;
        }

#if WE_LOVE_EM_SLASH_COMMANDS

        public static bool SetIsSlashCommandEnabled(string commandName, bool isEnabled, string reason = null)
        {
            var command = GetSlashCommandByCommandAlias(commandName.ToLower());

            if (command == null) return false;

            lock (StateSpecificSlashCommandHandlers)
            {
                if (!isEnabled && reason != null)
                    _disabledSlashCommandsReasons.Add((command, reason));
                else
                {
                    var list = _disabledSlashCommandsReasons.ToList();
                    list.RemoveAll(x => x.Item1 == command);
                    _disabledSlashCommandsReasons = list;
                }
                StateSpecificSlashCommandHandlers.Remove(command);

                command.IsEnabled = isEnabled;

                StateSpecificSlashCommandHandlers.Add(command);
            }

            return true;
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        public static bool SetIsEnabled(string commandName, bool isEnabled, string reason = null)
        {
            var command = GetCommandByCommandAlias(commandName.ToLower());

            if (command == null) return false;

            lock (StateSpecificCommandHandlers)
            {
                if (!isEnabled && reason != null)
                    _disabledCommandsReasons.Add((command, reason));
                else
                {
                    var list = _disabledCommandsReasons.ToList();
                    list.RemoveAll(x => x.Item1 == command);
                    _disabledCommandsReasons = list;
                }
                StateSpecificCommandHandlers.Remove(command);

                command.IsEnabled = isEnabled;

                StateSpecificCommandHandlers.Add(command);
            }

            return true;
        }

#if WE_LOVE_EM_SLASH_COMMANDS

        public static async Task CheckAndRunSlashCommand(SocketSlashCommand command)
        {
            var commandAlias = command.CommandName;

            InstrumentationPerfmon.CommandsPerSecond.Increment();

            var channel = command.Channel as SocketGuildChannel;
            var channelName = channel != null ? channel.Name.Escape() : command.Channel.Name;
            var channelId = channel?.Id ?? command.Channel.Id;
            var guildName = channel != null ? channel.Guild.Name.Escape() : $"Direct Message in {command.Channel.Name}.";
            var guildId = channel != null ? channel.Guild.Id : command.Channel.Id;
            var username = $"{command.User.Username.Escape()}#{command.User.Discriminator}";
            var userId = command.User.Id;

            InsertIntoAverages($"#{channelName} - {channelId}", $"{guildName} - {guildId}", $"{username} @ {userId}", commandAlias);
            Counters.RequestCountN++;
            SystemLogger.Singleton.Verbose(
                "Try execute the slash command '{0}' from '{1}' ({2}) in guild '{3}' ({4}) - channel '{5}' ({6}).",
                commandAlias,
                username,
                userId,
                guildName,
                guildId,
                channelName,
                channelId
            );

            await command.User.FireEventAsync(
                "SlashCommandExecuted",
                $"Try execute the slash command '{commandAlias}' from '{username}' ({userId}) in guild " +
                $"'{guildName}' ({guildId}) - channel '{channelName}' ({channelId})."
            );

            // May never get hit ever
            /*if (commandAlias.IsNullOrWhiteSpace())
            {
                SystemLogger.Singleton.Warning("We got a prefix in the message, but the command was 0 in length.");
                await command.User.FireEventAsync("InvalidSlashCommand", "Got prefix but command alias was empty");
                return;
            }*/

            var sw = Stopwatch.StartNew();
            var inNewThread = false;

            try
            {
                if (!_wasRegistered) RegisterOnce();

                var cmd = GetSlashCommandByCommandAlias(commandAlias);

                if (cmd == null)
                {
                    await command.User.FireEventAsync("SlashCommandNotFound", $"{channelId} {commandAlias}");
                    InstrumentationPerfmon.CommandsThatDidNotExist.Increment();
                    InstrumentationPerfmon.FailedCommandsPerSecond.Increment();
                    Counters.RequestFailedCountN++;
                    SystemLogger.Singleton.Warning("The slash command '{0}' did not exist.", commandAlias);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        InstrumentationPerfmon.NotFoundCommandsThatToldTheFrontendUser.Increment();
                        await command.RespondEphemeralAsync($"The slash command with the name '{commandAlias}' was not found.");
                    }
                    InstrumentationPerfmon.NotFoundCommandsThatDidNotTellTheFrontendUser.Increment();
                    return;
                }

                InstrumentationPerfmon.CommandsThatExist.Increment();

                if (!cmd.IsEnabled)
                {
                    var disabledMessage = (from c in _disabledSlashCommandsReasons select c.Item2).FirstOrDefault();

                    await command.User.FireEventAsync("SlashCommandDisabled", $"{channelId} {commandAlias} {disabledMessage ?? ""}");
                    InstrumentationPerfmon.CommandsThatAreDisabled.Increment();
                    SystemLogger.Singleton.Warning("The slash command '{0}' is disabled. {1}",
                        commandAlias,
                        disabledMessage != null
                            ? $"Because: '{disabledMessage}'"
                            : "");
                    var isAllowed = false;
                    if (command.User.IsAdmin())
                    {
                        if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminsToBypassDisabledCommands)
                        {
                            InstrumentationPerfmon.DisabledCommandsThatAllowedAdminBypass.Increment();
                            isAllowed = true;
                        }
                        else
                        {
                            InstrumentationPerfmon.DisabledCommandsThatDidNotAllowAdminBypass.Increment();
                        }
                    }

                    if (!isAllowed)
                    {
                        InstrumentationPerfmon.DisabledCommandsThatDidNotAllowBypass.Increment();
                        InstrumentationPerfmon.FailedCommandsPerSecond.Increment();
                        Counters.RequestFailedCountN++;
                        await command.RespondEphemeralAsync($"The command by the nameof '{commandAlias}' is disabled, please try again later. {(disabledMessage != null ? $"\nReason: '{disabledMessage}'" : "")}");
                        return;
                    }
                    InstrumentationPerfmon.DisabledCommandsThatWereInvokedToTheFrontendUser.Increment();
                }
                else
                {
                    InstrumentationPerfmon.CommandsThatAreEnabled.Increment();
                }

                InstrumentationPerfmon.CommandsThatPassedAllChecks.Increment();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExecuteCommandsInNewThread)
                {
                    InstrumentationPerfmon.CommandsThatTryToExecuteInNewThread.Increment();

                    var isAllowed = true;

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewThreadsOnlyAvailableForAdmins)
                    {
                        InstrumentationPerfmon.NewThreadCommandsThatAreOnlyAvailableToAdmins.Increment();
                        if (!command.User.IsAdmin())
                        {
                            InstrumentationPerfmon.NewThreadCommandsThatDidNotPassAdministratorCheck.Increment();
                            isAllowed = false;
                        }
                        else
                        {
                            InstrumentationPerfmon.NewThreadCommandsThatPassedAdministratorCheck.Increment();
                        }
                    }

                    if (isAllowed)
                    {
                        inNewThread = true;
                        InstrumentationPerfmon.NewThreadCommandsThatWereAllowedToExecute.Increment();
                        InstrumentationPerfmon.NewThreadCountersPerSecond.Increment();
                        ExecuteSlashCommandInNewThread(commandAlias, command, cmd, sw);
                        return;
                    }

                    InstrumentationPerfmon.NewThreadCommandsThatWereNotAllowedToExecute.Increment();
                }
                else
                {
                    InstrumentationPerfmon.CommandsThatDidNotTryNewThreadExecution.Increment();
                }

                await cmd.Invoke(command);

                InstrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                Counters.RequestSucceededCountN++;
            }
            catch (Exception ex)
            {
                await HandleSlashCommandException(ex, commandAlias, command);
            }
            finally
            {
                sw.Stop();
                InstrumentationPerfmon.AverageRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                InstrumentationPerfmon.CommandsThatFinished.Increment();
                SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'{2}.",
                    sw.Elapsed.TotalSeconds,
                    commandAlias,
                    inNewThread
                        ? " in new thread"
                        : "");
            }
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        public static async Task CheckAndRunCommandByAlias(string commandAlias, string[] messageContent, SocketMessage message)
        {

            InstrumentationPerfmon.CommandsPerSecond.Increment();


            var channel = message.Channel as SocketGuildChannel;
            var channelName = channel != null ? channel.Name.Escape() : message.Channel.Name;
            var channelId = channel?.Id ?? message.Channel.Id;
            var guildName = channel != null ? channel.Guild.Name.Escape() : $"Direct Message in {message.Channel.Name}.";
            var guildId = channel != null ? channel.Guild.Id : message.Channel.Id;
            var username = $"{message.Author.Username.Escape()}#{message.Author.Discriminator}";
            var userId = message.Author.Id;

            InsertIntoAverages($"#{channelName} - {channelId}", $"{guildName} - {guildId}", $"{username} @ {userId}", commandAlias);
            Counters.RequestCountN++;
            SystemLogger.Singleton.Verbose(
                "Try execute the command '{0}' with the arguments '{1}' from '{2}' ({3}) in guild '{4}' ({5}) - channel '{6}' ({7}).",
                commandAlias,
                messageContent.Length > 0
                    ? messageContent.Join(' ')
                        .EscapeNewLines()
                        .Escape()
                    : "No command arguments.",
                username,
                userId,
                guildName,
                guildId,
                channelName,
                channelId
            );

            await message.Author.PageViewedAsync($"{typeof(CommandRegistry).FullName}({message.Channel.Name})");

            await message.Author.FireEventAsync(
                "CommandExecuted",
                $"Tried to execute the command '{commandAlias}' with the arguments " +
                $"'{(messageContent.Length > 0 ? messageContent.Join(' ').EscapeNewLines().Escape() : "No command arguments.")}' " +
                $"in guild '{guildName}' ({guildId}) - channel '{channelName}' ({channelId})."
            );

            if (commandAlias.IsNullOrWhiteSpace())
            {
                SystemLogger.Singleton.Warning("We got a prefix in the message, but the command was 0 in length.");
                await message.Author.FireEventAsync("InvalidCommand", "Got prefix but command alias was empty");
                return;
            }

            if (
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.CommandRegistryOnlyMatchAlphabetCharactersForCommandName &&
                !Regex.IsMatch(commandAlias, @"^[a-zA-Z-]*$") && 
                !message.Author.IsAdmin()
            )
            {
                SystemLogger.Singleton.Warning("We got a prefix in the message, but the command contained non-alphabetic characters, message: {0}", commandAlias);
                await message.Author.FireEventAsync("InvalidCommand", "The command did not contain alphabet characters.");
                // should we reply here?
                return;
            }

            var sw = Stopwatch.StartNew();
            var inNewThread = false;

            try
            {
                if (!_wasRegistered) RegisterOnce();

                var command = GetCommandByCommandAlias(commandAlias);

                if (command == null)
                {
                    await message.Author.FireEventAsync("CommandNotFound", $"{channelId} {commandAlias}");
                    InstrumentationPerfmon.CommandsThatDidNotExist.Increment();
                    InstrumentationPerfmon.FailedCommandsPerSecond.Increment();
                    Counters.RequestFailedCountN++;
                    SystemLogger.Singleton.Warning("The command '{0}' did not exist.", commandAlias);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        InstrumentationPerfmon.NotFoundCommandsThatToldTheFrontendUser.Increment();
                        await message.ReplyAsync($"The command with the name '{commandAlias}' was not found.");
                        return;
                    }
                    InstrumentationPerfmon.NotFoundCommandsThatDidNotTellTheFrontendUser.Increment();
                    return;
                }

                InstrumentationPerfmon.CommandsThatExist.Increment();

                if (!command.IsEnabled)
                {
                    var disabledMessage =
                        (from cmd in _disabledCommandsReasons where cmd.Item1 == command select cmd.Item2)
                        .FirstOrDefault();

                    await message.Author.FireEventAsync("CommandDisabled", $"{channelId} {commandAlias} {disabledMessage ?? ""}");
                    InstrumentationPerfmon.CommandsThatAreDisabled.Increment();
                    SystemLogger.Singleton.Warning("The command '{0}' is disabled. {1}",
                        commandAlias,
                        disabledMessage != null
                            ? $"Because: '{disabledMessage}'"
                            : "");
                    var isAllowed = false;
                    if (message.Author.IsAdmin())
                    {
                        if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminsToBypassDisabledCommands)
                        {
                            InstrumentationPerfmon.DisabledCommandsThatAllowedAdminBypass.Increment();
                            isAllowed = true;
                        }
                        else
                        {
                            InstrumentationPerfmon.DisabledCommandsThatDidNotAllowAdminBypass.Increment();
                        }
                    }

                    if (!isAllowed)
                    {
                        InstrumentationPerfmon.DisabledCommandsThatDidNotAllowBypass.Increment();
                        InstrumentationPerfmon.FailedCommandsPerSecond.Increment();
                        Counters.RequestFailedCountN++;
                        await message.ReplyAsync($"The command by the nameof '{commandAlias}' is disabled, please try again later. {(disabledMessage != null ? $"\nReason: '{disabledMessage}'" : "")}");
                        return;
                    }
                    InstrumentationPerfmon.DisabledCommandsThatWereInvokedToTheFrontendUser.Increment();
                }
                else
                {
                    InstrumentationPerfmon.CommandsThatAreEnabled.Increment();
                }

                InstrumentationPerfmon.CommandsThatPassedAllChecks.Increment();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExecuteCommandsInNewThread)
                {
                    InstrumentationPerfmon.CommandsThatTryToExecuteInNewThread.Increment();

                    var isAllowed = true;

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewThreadsOnlyAvailableForAdmins)
                    {
                        InstrumentationPerfmon.NewThreadCommandsThatAreOnlyAvailableToAdmins.Increment();
                        if (!message.Author.IsAdmin())
                        {
                            InstrumentationPerfmon.NewThreadCommandsThatDidNotPassAdministratorCheck.Increment();
                            isAllowed = false;
                        }
                        else
                        {
                            InstrumentationPerfmon.NewThreadCommandsThatPassedAdministratorCheck.Increment();
                        }
                    }

                    if (isAllowed)
                    {
                        inNewThread = true;
                        InstrumentationPerfmon.NewThreadCommandsThatWereAllowedToExecute.Increment();
                        InstrumentationPerfmon.NewThreadCountersPerSecond.Increment();
                        ExecuteCommandInNewThread(commandAlias, messageContent, message, sw, command);
                        return;
                    }

                    InstrumentationPerfmon.NewThreadCommandsThatWereNotAllowedToExecute.Increment();
                }
                else
                {
                    InstrumentationPerfmon.CommandsThatDidNotTryNewThreadExecution.Increment();
                }

                InstrumentationPerfmon.CommandsNotExecutedInNewThread.Increment();

                await message.Author.PageViewedAsync($"{commandAlias}({message.Channel.Name})");

                await GetWrapperByCommand(command)
                    .ExecuteAsync(messageContent, message, commandAlias);

                InstrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                Counters.RequestSucceededCountN++;
            }
            catch (Exception ex)
            {
                await HandleException(ex, commandAlias, message);
            }
            finally
            {
                sw.Stop();
                InstrumentationPerfmon.AverageRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                InstrumentationPerfmon.CommandsThatFinished.Increment();
                SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'{2}.",
                    sw.Elapsed.TotalSeconds,
                    commandAlias,
                    inNewThread
                        ? " in new thread"
                        : "");
            }
        }

#if WE_LOVE_EM_SLASH_COMMANDS

        private static void ExecuteSlashCommandInNewThread(string alias, SocketSlashCommand command, IStateSpecificSlashCommandHandler handler, Stopwatch sw)
        {
            SystemLogger.Singleton.LifecycleEvent("Queueing user work item for slash command '{0}'.", alias);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    InstrumentationPerfmon.NewThreadCommandsThatPassedChecks.Increment();
                    // We do not expect a result here.
                    await handler.Invoke(command);
                    InstrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                    Counters.RequestSucceededCountN++;
                }
                catch (Exception ex)
                {
                    await HandleSlashCommandException(ex, alias, command);
                }
                finally
                {
                    sw.Stop();
                    InstrumentationPerfmon.AverageThreadRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                    InstrumentationPerfmon.NewThreadCommandsThatFinished.Increment();
                    SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'.", sw.Elapsed.TotalSeconds, alias);
                }

            });
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        private static void ExecuteCommandInNewThread(
            string alias,
            string[] messageContent,
            SocketMessage message,
            Stopwatch sw,
            IStateSpecificCommandHandler command
        )
        {
            SystemLogger.Singleton.LifecycleEvent("Queueing user work item for command '{0}'.", alias);

            // could we have 2 versions here where we pool it and background it?

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    InstrumentationPerfmon.NewThreadCommandsThatPassedChecks.Increment();
                    await message.Author.PageViewedAsync($"{alias}({message.Channel.Name})");
                    // We do not expect a result here.
                    await GetWrapperByCommand(command)
                        .ExecuteAsync(
                            messageContent,
                            message,
                            alias
                        );
                    InstrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                    Counters.RequestSucceededCountN++;
                }
                catch (Exception ex)
                {
                    await HandleException(ex, alias, message);
                }
                finally
                {
                    sw.Stop();
                    InstrumentationPerfmon.AverageThreadRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                    InstrumentationPerfmon.NewThreadCommandsThatFinished.Increment();
                    SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'.", sw.Elapsed.TotalSeconds, alias);
                }
            });
        }

#if WE_LOVE_EM_SLASH_COMMANDS

        private static async Task HandleSlashCommandException(Exception ex, string alias, SocketSlashCommand command)
        {
            InstrumentationPerfmon.FailedCommandsPerSecond.Increment();
            Counters.RequestFailedCountN++;
            await command.User.FireEventAsync("SlashCommandException", $"The command {alias} threw: {ex.ToDetailedString()}");

            var exceptionId = Guid.NewGuid();

            switch (ex)
            {
                case NotSupportedException _:
                    SystemLogger.Singleton.Warning("This could have been a thread pool error, we'll assume that.");
                    return;
                case ApplicationException _:
                    SystemLogger.Singleton.Warning("Application threw an exception {0}", ex.ToDetailedString());
                    await command.RespondEphemeralAsync($"The command threw an exception: {ex.Message}");
                    return;
                case TimeoutException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    SystemLogger.Singleton.Error("The command '{0}' timed out. {1}", alias, ex.Message);
                    await command.RespondEphemeralAsync("the command you tried to execute has timed out, please " +
                                                        "try identify the leading cause of a timeout.");
                    return;
                case EndpointNotFoundException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    InstrumentationPerfmon.FailedCommandsThatTriedToAccessOfflineGridServer.Increment();
                    SystemLogger.Singleton.Warning("The grid service was not online.");
                    await command.RespondEphemeralAsync($"the grid service is not currently running, " +
                                                        $"please ask <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}>" +
                                                        $" to start the service.");
                    return;
                case FaultException fault:
                {
                    InstrumentationPerfmon.FailedCommandsThatTriggeredAFaultException.Increment();
                    SystemLogger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                    if (fault.Message == "Cannot invoke BatchJob while another job is running")
                    {
                        InstrumentationPerfmon.FailedFaultCommandsThatWereDuplicateInvocations.Increment();
                        await command.RespondEphemeralAsync("You are sending requests too fast, please slow down!");
                        return;
                    }

                    if (fault.Message == "BatchJob Timeout")
                    {
                        InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                        await command.RespondEphemeralAsync("The job timed out, please try again later.");
                        return;
                    }

                    InstrumentationPerfmon.FailedFaultCommandsThatWereNotDuplicateInvocations.Increment();
                    InstrumentationPerfmon.FailedFaultCommandsThatWereLuaExceptions.Increment();

                    if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                    {
                        await command.RespondWithFileEphemeralAsync(
                            new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                            "fault.txt",
                            "An exception occurred on the grid server, please review this error to see if your " +
                            "input was malformed:");
                        return;
                    }

                    await command.RespondEphemeralAsync(
                        "An exception occurred on the grid server, please review this error to see if your input " +
                        "was malformed:",
                        embed: new EmbedBuilder()
                            .WithColor(0xff, 0x00, 0x00)
                            .WithTitle("GridServer exception.")
                            .WithAuthor(command.User)
                            .WithDescription($"```\n{fault.Message}\n```")
                            .Build()
                    );
                    return;
                }
            }

            InstrumentationPerfmon.FailedCommandsThatWereUnknownExceptions.Increment();

            SystemLogger.Singleton.Error("[EID-{0}] An unexpected error occurred: {1}", exceptionId.ToString(), ex.ToDetailedString());

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await command.RespondWithFileEphemeralAsync(new MemoryStream(Encoding.UTF8.GetBytes(detail)), 
                        "ex.txt");
                    return;
                }

                InstrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await command.RespondEphemeralAsync(
                    "An error occured with the script execution task and the environment variable 'CareToLeakSensitiveExceptions' " +
                    "is false, this may leak sensitive information:",
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }

            InstrumentationPerfmon.FailedCommandsThatWerePublicallyMasked.Increment();

            await command.RespondEphemeralAsync(
                $"An unexpected Exception has occurred. Exception ID: {exceptionId}, send this ID to " +
                $"<@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}>");
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        private static async Task HandleException(Exception ex, string alias, SocketMessage message)
        {
            InstrumentationPerfmon.FailedCommandsPerSecond.Increment();
            Counters.RequestFailedCountN++;
            await message.Author.FireEventAsync("CommandException", $"The command {alias} threw: {ex.ToDetailedString()}");

            var exceptionId = Guid.NewGuid();

            if (ex is not FaultException)
                global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex);

            switch (ex)
            {
                case CircuitBreakerException _:
                    SystemLogger.Singleton.Warning("CircuitBreakerException '{0}'", ex.ToDetailedString());
                    await message.ReplyAsync(ex.Message);
                    return;
                case NotSupportedException _:
                    SystemLogger.Singleton.Warning("This could have been a thread pool error, we'll assume that.");
                    return;
                case ApplicationException _:
                    SystemLogger.Singleton.Warning("Application threw an exception {0}", ex.ToDetailedString());
                    await message.ReplyAsync($"The command threw an exception: {ex.Message}");
                    return;
                case TimeoutException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    SystemLogger.Singleton.Error("The command '{0}' timed out. {1}", alias, ex.Message);
                    await message.ReplyAsync("the command you tried to execute has timed out, please try identify " +
                                             "the leading cause of a timeout.");
                    return;
                case EndpointNotFoundException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    InstrumentationPerfmon.FailedCommandsThatTriedToAccessOfflineGridServer.Increment();
                    SystemLogger.Singleton.Warning("The grid service was not online.");
                    await message.ReplyAsync($"the grid service is not currently running, please ask " +
                                             $"<@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}> to start " +
                                             $"the service.");
                    return;
                case FaultException fault:
                {
                    InstrumentationPerfmon.FailedCommandsThatTriggeredAFaultException.Increment();
                    SystemLogger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                    switch (fault.Message)
                    {
                        case "Cannot invoke BatchJob while another job is running":
                            InstrumentationPerfmon.FailedFaultCommandsThatWereDuplicateInvocations.Increment();
                            await message.ReplyAsync("You are sending requests too fast, please slow down!");
                            return;
                        case "BatchJob Timeout":
                            InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                            await message.ReplyAsync("The job timed out, please try again later.");
                            return;
                    }

                    InstrumentationPerfmon.FailedFaultCommandsThatWereNotDuplicateInvocations.Increment();
                    InstrumentationPerfmon.FailedFaultCommandsThatWereLuaExceptions.Increment();

                    if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                    {
                        await message.Channel.SendFileAsync(
                            new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)),
                            "fault.txt",
                            "An exception occurred on the grid server, please review this error to see if " +
                            "your input was malformed:");
                        return;
                    }

                    await message.Channel.SendMessageAsync(
                        "An exception occurred on the grid server, please review this error to see if" +
                        " your input was malformed:",
                        embed: new EmbedBuilder()
                            .WithColor(0xff, 0x00, 0x00)
                            .WithTitle("GridServer exception.")
                            .WithAuthor(message.Author)
                            .WithDescription($"```\n{fault.Message}\n```")
                            .Build()
                    );
                    return;
                }
            }

            InstrumentationPerfmon.FailedCommandsThatWereUnknownExceptions.Increment();

            SystemLogger.Singleton.Error("[EID-{0}] An unexpected error occurred: {1}",
                exceptionId.ToString(),
                ex.ToDetailedString());

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await message.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt");
                    return;
                }

                InstrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await message.ReplyAsync(
                    "An error occured with the script execution task and the environment variable " +
                    "'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }

            InstrumentationPerfmon.FailedCommandsThatWerePublicallyMasked.Increment();

            await message.ReplyAsync($"An unexpected Exception has occurred. Exception ID: {exceptionId}, " +
                                     $"send this ID to <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}>");
        }

        private static IStateSpecificCommandHandler GetCommandByCommandAlias(string alias)
        {
            lock (StateSpecificCommandHandlers)
                return (from command in StateSpecificCommandHandlers where command.CommandAliases.Contains(alias) select command).FirstOrDefault();
        }

        private static CommandCircuitBreakerWrapper GetWrapperByCommand(IStateSpecificCommandHandler command)
        {
            lock (CircuitBreakerWrappers)
                return (from wrapper in CircuitBreakerWrappers
                    where wrapper.Command.CommandName == command.CommandName
                    select wrapper).FirstOrDefault();
        }

#if WE_LOVE_EM_SLASH_COMMANDS

        private static IStateSpecificSlashCommandHandler GetSlashCommandByCommandAlias(string alias)
        {
            lock (StateSpecificSlashCommandHandlers)
                return (from command in StateSpecificSlashCommandHandlers where command.CommandAlias == alias select command).FirstOrDefault();
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        private static void ParseAndInsertIntoCommandRegistry()
        {
            lock (StateSpecificCommandHandlers)
            {
                InstrumentationPerfmon.CommandsParsedAndInsertedIntoRegistry.Increment();
                SystemLogger.Singleton.LifecycleEvent("Begin attempt to register commands via Reflection");

                try
                {
                    var defaultCommandNamespace = GetDefaultCommandNamespace();

                    SystemLogger.Singleton.Info("Got default command namespace '{0}'.", defaultCommandNamespace);

                    var defaultCommandTypes = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(defaultCommandNamespace);

#if WE_LOVE_EM_SLASH_COMMANDS

                    var slashCommandNamespace = GetSlashCommandNamespace();
                    SystemLogger.Singleton.Info("Got slash command namespace '{0}'.", slashCommandNamespace);
                    var slashCommandTypes = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(slashCommandNamespace);


                    if (slashCommandTypes.Length == 0)
                    {
                        InstrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                        SystemLogger.Singleton.Warning("There were no slash commands found in the namespace '{0}'.", slashCommandNamespace);
                    }
                    else
                    {
                        foreach (var type in slashCommandTypes)
                        {
                            if (type.IsClass)
                            {
                                var commandHandler = Activator.CreateInstance(type);
                                
                                if (!(commandHandler is IStateSpecificSlashCommandHandler trueCommandHandler)) continue;
                                
                                SystemLogger.Singleton.Info("Parsing slash command '{0}'.", type.FullName);

                                if (trueCommandHandler.CommandAlias.IsNullOrEmpty())
                                {
                                    InstrumentationPerfmon.StateSpecificCommandsThatHadNoAliases.Increment();
                                    SystemLogger.Singleton.Trace(
                                        "Exception when reading '{0}': Expected the sizeof field 'CommandAlias' " +
                                        "to not be null or empty",
                                        type.FullName
                                    );

                                    continue;
                                }
                                if (trueCommandHandler.CommandName.IsNullOrEmpty())
                                {
                                    InstrumentationPerfmon.StateSpecificCommandsThatHadNoName.Increment();
                                    SystemLogger.Singleton.Trace(
                                        "Exception when reading '{0}': Expected field 'CommandName' to be not null",
                                        type.FullName
                                    );

                                    continue;
                                }

                                if (trueCommandHandler.CommandDescription is {Length: 0})
                                {
                                    InstrumentationPerfmon.StateSpecificCommandsThatHadNoNullButEmptyDescription.Increment();
                                    SystemLogger.Singleton.Warning(
                                        "Exception when reading '{0}': Expected field 'CommandDescription' " +
                                        "to have a size greater than 0",
                                        type.FullName
                                    );
                                }

                                InstrumentationPerfmon.StateSpecificCommandsThatWereAddedToTheRegistry.Increment();

                                var builder = new SlashCommandBuilder();

                                builder.WithName(trueCommandHandler.CommandAlias);
                                builder.WithDescription($"{trueCommandHandler.CommandName} " +
                                                        $"({trueCommandHandler.CommandAlias}):" +
                                                        $" {trueCommandHandler.CommandDescription}");
                                builder.WithDefaultPermission(true);

                                if (trueCommandHandler.Options != null)
                                    builder.AddOptions(trueCommandHandler.Options);

                                if (trueCommandHandler.GuildId != null)
                                {
                                    var guild = BotGlobal.Singleton.Client.GetGuild(trueCommandHandler.GuildId.Value);

                                    if (guild == null)
                                    {
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Unknown Guild '{1}'",
                                            type.FullName,
                                            trueCommandHandler.GuildId
                                        );

                                        continue;
                                    }

                                    guild.CreateApplicationCommand(builder.Build());
                                }
                                else
                                {
                                    BotGlobal.Singleton.Client.CreateGlobalApplicationCommand(builder.Build());
                                }

                                StateSpecificSlashCommandHandlers.Add(trueCommandHandler);
                            }
                            else
                            {
                                InstrumentationPerfmon.CommandsInNamespaceThatWereNotClasses.Increment();
                            }
                        }
                    }

#endif // WE_LOVE_EM_SLASH_COMMANDS

                    if (defaultCommandTypes.Length == 0)
                    {
                        InstrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                        SystemLogger.Singleton.Warning("There were no default commands found in the namespace '{0}'.", 
                            defaultCommandNamespace);
                    }
                    else
                    {
                        foreach (var type in defaultCommandTypes)
                        {
                            if (type.IsClass)
                            {
                                var commandHandler = Activator.CreateInstance(type);
                                if (commandHandler is IStateSpecificCommandHandler trueCommandHandler)
                                {
                                    SystemLogger.Singleton.Info("Parsing command '{0}'.", type.FullName);

                                    if (trueCommandHandler.CommandAliases.Length < 1)
                                    {
                                        InstrumentationPerfmon.StateSpecificCommandsThatHadNoAliases.Increment();
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Expected the sizeof field " +
                                            "'CommandAliases' to be greater than 0, got {1}",
                                            type.FullName,
                                            trueCommandHandler.CommandAliases.Length
                                        );

                                        continue;
                                    }

                                    if (trueCommandHandler.CommandName.IsNullOrEmpty())
                                    {
                                        InstrumentationPerfmon.StateSpecificCommandsThatHadNoName.Increment();
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Expected field 'CommandName' to be not null",
                                            type.FullName
                                        );

                                        continue;
                                    }

                                    if (trueCommandHandler.CommandDescription is {Length: 0})
                                    {
                                        InstrumentationPerfmon.StateSpecificCommandsThatHadNoNullButEmptyDescription.Increment();
                                        SystemLogger.Singleton.Warning(
                                            "Exception when reading '{0}': Expected field " +
                                            "'CommandDescription' to have a size greater than 0",
                                            type.FullName
                                        );
                                    }

                                    InstrumentationPerfmon.StateSpecificCommandsThatWereAddedToTheRegistry.Increment();



                                    StateSpecificCommandHandlers.Add(trueCommandHandler);
                                    CircuitBreakerWrappers.Add(new CommandCircuitBreakerWrapper(trueCommandHandler));
                                }
                                else
                                {
                                    InstrumentationPerfmon.CommandThatWereNotStateSpecific.Increment();
                                }
                            }
                            else
                            {
                                InstrumentationPerfmon.CommandsInNamespaceThatWereNotClasses.Increment();
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    InstrumentationPerfmon.CommandRegistryRegistrationsThatFailed.Increment();
                    SystemLogger.Singleton.Error(ex);
                }
                finally
                {
                    SystemLogger.Singleton.Verbose("Successfully initialized the CommandRegistry.");
                }
            }
        }



        public static void RegisterOnce()
        {
            if (_wasRegistered) return;
            
            lock (RegistrationLock)
            {
                ParseAndInsertIntoCommandRegistry();
                _wasRegistered = true;
            }
        }

        #region Legacy Metrics

        public static (Modes, CountersData) GetMetrics()
        {
            return (CalculateModes(), Counters);
        }

        public static void LogMetricsReport()
        {
            SystemLogger.Singleton.Warning(
                "Command Registry metrics report for Date ({0} at {1})",
                DateTimeGlobal.GetUtcNowAsIso(),
                LoggingSystem.GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("f7")
            );
            SystemLogger.Singleton.Log("=====================================================================================");
            SystemLogger.Singleton.Log("Total command request count: {0}", Counters.RequestCountN);
            SystemLogger.Singleton.Log("Total succeeded command request count: {0}", Counters.RequestSucceededCountN);
            SystemLogger.Singleton.Log("Total failed command request count: {0}", Counters.RequestFailedCountN);

            var modes = CalculateModes();

            SystemLogger.Singleton.Log("Average request channel: '{0}' with average of {1}", modes.Channels.Item, modes.Channels.Average);
            SystemLogger.Singleton.Log("Average request guild: '{0}' with average of {1}", modes.Servers.Item, modes.Servers.Average);
            SystemLogger.Singleton.Log("Average request user: '{0}' with average of {1}", modes.Users.Item, modes.Users.Average);
            SystemLogger.Singleton.Log("Average request command name: '{0}' with average of {1}", modes.Commands.Item, modes.Commands.Average);

            SystemLogger.Singleton.Log("=====================================================================================");
        }

        private static void InsertIntoAverages(string channelName, string serverName, string userName, string commandName)
        {
            Averages.Channels.Add(channelName);
            Averages.Servers.Add(serverName);
            Averages.Users.Add(userName);
            Averages.Commands.Add(commandName);
        }

        private static Modes CalculateModes() =>
            new()
            {
                Channels = CalculateModeOfArray(Averages.Channels),
                Servers = CalculateModeOfArray(Averages.Servers),
                Users = CalculateModeOfArray(Averages.Users),
                Commands = CalculateModeOfArray(Averages.Commands)
            };

        private static readonly CountersData Counters = new();
        private static readonly AveragesData Averages = new();

        private class AveragesData
        {
            internal readonly ICollection<string> Channels = new List<string>();
            internal readonly ICollection<string> Servers = new List<string>();
            internal readonly ICollection<string> Users = new List<string>();
            internal readonly ICollection<string> Commands = new List<string>();
        }

        public class CountersData
        {
            public int RequestCountN;
            public int RequestFailedCountN;
            public int RequestSucceededCountN;
        }

        public class Modes
        {
            public Mode<string> Channels;
            public Mode<string> Servers;
            public Mode<string> Users;
            public Mode<string> Commands;
        }

        private static Mode<T> CalculateModeOfArray<T>(IEnumerable<T> collection)
        {
            try
            {
                var array = collection.ToArray();

                if (array.Length == 0)
                    return new Mode<T>
                    {
                        Item = default,
                        Average = 0
                    };

                var mf = 1;
                var m = 0;
                T item = default;
                for (var i = 0; i < array.Length; i++)
                {
                    for (var j = i; j < array.Length; j++)
                    {
                        if (array[i].Equals(array[j])) m++;
                        if (mf >= m) continue;
                        mf = m;
                        item = array[i];
                    }
                    m = 0;
                }

                return new Mode<T>
                {
                    Item = EqualityComparer<T>.Default.Equals(item, default) ? array[array.Length - 1] : item,
                    Average = mf
                };
            }
            catch
            {
                return new Mode<T>
                {
                    Item = default,
                    Average = 0
                };
            }
        }

        public struct Mode<T>
        {
            public T Item;
            public int Average;
        }

        #endregion Legacy Metrics
    }
}
