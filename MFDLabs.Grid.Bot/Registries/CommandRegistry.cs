/*
    Name: CommandRegistry.cs
    Written By: Alex Bkordan
    Description: C# Runtime parser for a command registry
*/

// Jakob: TODO, Load these commands from a different assembly so they can be changed at runtime?
//              It will have to load the assembly, and this shouldn't have a reference to it.
// Alex: That won't really work if we want to keep up shared settings.


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
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Logging;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Registries
{
    public sealed class CommandRegistry : SingletonBase<CommandRegistry>
    {
        private bool wasRegistered = false;

        private readonly object _registrationLock = new object();

        private readonly ICollection<IStateSpecificCommandHandler> _stateSpecificCommandHandlers = new List<IStateSpecificCommandHandler>();
        private ICollection<(IStateSpecificCommandHandler, string)> _disabledCommandsReasons = new List<(IStateSpecificCommandHandler, string)>();

        private readonly ICollection<IStateSpecificSlashCommandHandler> _stateSpecificSlashCommandHandlers = new List<IStateSpecificSlashCommandHandler>();
        private ICollection<(IStateSpecificSlashCommandHandler, string)> _disabledSlashCommandsReasons = new List<(IStateSpecificSlashCommandHandler, string)>();

        private readonly CommandRegistryInstrumentationPerformanceMonitor _instrumentationPerfmon = new CommandRegistryInstrumentationPerformanceMonitor(PerfmonCounterRegistryProvider.Registry);

        private string GetDefaultCommandNamespace()
        {
            return $"{typeof(Program).Namespace}.Commands";
        }

        private string GetSlashCommandNamespace()
        {
            return $"{typeof(Program).Namespace}.SlashCommands";
        }

        public Embed ConstructHelpEmbedForSingleCommand(string commandName, IUser author)
        {
            if (!wasRegistered) RegisterOnce();

            var command = GetCommandByCommandAlias(commandName);

            if (command == default) return null;

            var isInternal = command.Internal == true;
            var isDisabled = command.IsEnabled == false;

            if (isInternal && !author.IsAdmin()) return null;

            var builder = new EmbedBuilder
            {
                Title = $"{command.CommandName} Documentation"
            };
            if (isDisabled)
            {
                builder.Color = new Color(0xff, 0x00, 0x00);
            }
            else
            {
                builder.Color = new Color(0x00, 0xff, 0x00);
            }
            builder.AddField(string.Join(", ", command.CommandAliases), $"{command.CommandDescription}\n{(isInternal ? ":no_entry:" : "")} {(isDisabled ? ":x:" : ":white_check_mark:")}", false);
            builder.WithCurrentTimestamp();
            builder.Description = ":no_entry:\\: **INTERNAL**\n:x:\\: **DISABLED**\n:white_check_mark:\\: **ENABLED**";

            return builder.Build();
        }

        public ICollection<Embed> ConstructHelpEmbedForAllCommands(IUser author)
        {
            if (!wasRegistered) RegisterOnce();

            var builder = new EmbedBuilder().WithTitle("Documentation");
            var embeds = new List<Embed>();
            var i = 0;

            foreach (var command in _stateSpecificCommandHandlers)
            {
                if (i == 24)
                {
                    embeds.Add(builder.Build());
                    builder = new EmbedBuilder();
                    i = 0;
                }

                var isInternal = command.Internal == true;
                var isDisabled = command.IsEnabled == false;
                if (isInternal && !author.IsAdmin()) continue;

                builder.AddField(
                    $"{command.CommandName}: {string.Join(", ", command.CommandAliases)}",
                    $"{command.CommandDescription}\n{(isInternal ? ":no_entry:" : "")} {(isDisabled ? ":x:" : ":white_check_mark:")}",
                    false
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

        public bool SetIsEnabled(string commandName, bool isEnabled, string reason = null)
        {
            var command = GetCommandByCommandAlias(commandName.ToLower());

            if (command == null) return false;

            lock (_stateSpecificCommandHandlers)
            {
                if (!isEnabled && reason != null)
                    _disabledCommandsReasons.Add((command, reason));
                else
                {
                    var list = _disabledCommandsReasons.ToList();
                    list.RemoveAll(x => x.Item1 == command);
                    _disabledCommandsReasons = list;
                }
                _stateSpecificCommandHandlers.Remove(command);

                command.IsEnabled = isEnabled;

                _stateSpecificCommandHandlers.Add(command);
            }

            return true;
        }

        public async Task CheckAndRunSlashCommand(SocketSlashCommand command)
        {
            var commandAlias = command.CommandName;

            _instrumentationPerfmon.CommandsPerSecond.Increment();

            var channel = command.Channel as SocketGuildChannel;
            var channelName = channel != null ? channel.Name.Escape() : command.Channel.Name;
            var channelId = channel == null ? command.Channel.Id : channel.Id;
            var guildName = channel != null ? channel.Guild.Name.Escape() : $"Direct Message in {command.Channel.Name}.";
            var guildId = channel != null ? channel.Guild.Id : command.Channel.Id;
            var username = $"{command.User.Username.Escape()}#{command.User.Discriminator}";
            var userId = command.User.Id;

            InsertIntoAverages($"#{channelName} - {channelId}", $"{guildName} - {guildId}", $"{username} @ {userId}", commandAlias);
            _counters.RequestCountN++;
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
                string.Format(
                    "Try execute the slash command '{0}' from '{1}' ({2}) in guild '{3}' ({4}) - channel '{5}' ({6}).",
                    commandAlias,
                    username,
                    userId,
                    guildName,
                    guildId,
                    channelName,
                    channelId
                )
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
                if (!wasRegistered) RegisterOnce();

                var cmd = GetSlashCommandByCommandAlias(commandAlias);

                if (cmd == null)
                {
                    await command.User.FireEventAsync("SlashCommandNotFound", $"{channelId} {commandAlias}");
                    _instrumentationPerfmon.CommandsThatDidNotExist.Increment();
                    _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
                    _counters.RequestFailedCountN++;
                    SystemLogger.Singleton.Warning("The slash command '{0}' did not exist.", commandAlias);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        _instrumentationPerfmon.NotFoundCommandsThatToldTheFrontendUser.Increment();
                        await command.RespondEphemeralAsync($"The slash command with the name '{commandAlias}' was not found.");
                    }
                    _instrumentationPerfmon.NotFoundCommandsThatDidNotTellTheFrontendUser.Increment();
                    return;
                }

                _instrumentationPerfmon.CommandsThatExist.Increment();

                if (!cmd.IsEnabled)
                {
                    var disabledMessage = (from c in _disabledSlashCommandsReasons where c.Item1 == command select c.Item2).FirstOrDefault();

                    await command.User.FireEventAsync("SlashCommandDisabled", $"{channelId} {commandAlias} {disabledMessage ?? ""}");
                    _instrumentationPerfmon.CommandsThatAreDisabled.Increment();
                    SystemLogger.Singleton.Warning("The slash command '{0}' is disabled. {1}", commandAlias, disabledMessage != null ? $"Because: '{disabledMessage}'" : "");
                    bool isAllowed = false;
                    if (command.User.IsAdmin())
                    {
                        if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminsToBypassDisabledCommands)
                        {
                            _instrumentationPerfmon.DisabledCommandsThatAllowedAdminBypass.Increment();
                            isAllowed = true;
                        }
                        else
                        {
                            _instrumentationPerfmon.DisabledCommandsThatDidNotAllowAdminBypass.Increment();
                            isAllowed = false;
                        }
                    }

                    if (!isAllowed)
                    {
                        _instrumentationPerfmon.DisabledCommandsThatDidNotAllowBypass.Increment();
                        _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
                        _counters.RequestFailedCountN++;
                        await command.RespondEphemeralAsync($"The command by the nameof '{commandAlias}' is disabled, please try again later. {(disabledMessage != null ? $"\nReason: '{disabledMessage}'" : "")}");
                        return;
                    }
                    _instrumentationPerfmon.DisabledCommandsThatWereInvokedToTheFrontendUser.Increment();
                }
                else
                {
                    _instrumentationPerfmon.CommandsThatAreEnabled.Increment();
                }

                _instrumentationPerfmon.CommandsThatPassedAllChecks.Increment();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExecuteCommandsInNewThread)
                {
                    _instrumentationPerfmon.CommandsThatTryToExecuteInNewThread.Increment();

                    var isAllowed = true;

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewThreadsOnlyAvailableForAdmins)
                    {
                        _instrumentationPerfmon.NewThreadCommandsThatAreOnlyAvailableToAdmins.Increment();
                        if (!command.User.IsAdmin())
                        {
                            _instrumentationPerfmon.NewThreadCommandsThatDidNotPassAdministratorCheck.Increment();
                            isAllowed = false;
                        }
                        else
                        {
                            _instrumentationPerfmon.NewThreadCommandsThatPassedAdministratorCheck.Increment();
                            isAllowed = true;
                        }
                    }

                    if (isAllowed)
                    {
                        inNewThread = true;
                        _instrumentationPerfmon.NewThreadCommandsThatWereAllowedToExecute.Increment();
                        _instrumentationPerfmon.NewThreadCountersPerSecond.Increment();
                        ExecuteSlashCommandInNewThread(commandAlias, command, cmd, sw);
                        return;
                    }

                    _instrumentationPerfmon.NewThreadCommandsThatWereNotAllowedToExecute.Increment();
                }
                else
                {
                    _instrumentationPerfmon.CommandsThatDidNotTryNewThreadExecution.Increment();
                }

                await cmd.Invoke(command);

                _instrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                _counters.RequestSucceededCountN++;
            }
            catch (Exception ex)
            {
                await HandleSlashCommandException(ex, commandAlias, command);
            }
            finally
            {
                sw.Stop();
                _instrumentationPerfmon.AverageRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                _instrumentationPerfmon.CommandsThatFinished.Increment();
                SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'{2}.", sw.Elapsed.TotalSeconds, commandAlias, inNewThread ? " in new thread" : "");
            }

            return;
        }

        public async Task CheckAndRunCommandByAlias(string commandAlias, string[] messageContent, SocketMessage message)
        {

            _instrumentationPerfmon.CommandsPerSecond.Increment();


            var channel = message.Channel as SocketGuildChannel;
            var channelName = channel != null ? channel.Name.Escape() : message.Channel.Name;
            var channelId = channel == null ? message.Channel.Id : channel.Id;
            var guildName = channel != null ? channel.Guild.Name.Escape() : $"Direct Message in {message.Channel.Name}.";
            var guildId = channel != null ? channel.Guild.Id : message.Channel.Id;
            var username = $"{message.Author.Username.Escape()}#{message.Author.Discriminator}";
            var userId = message.Author.Id;

            InsertIntoAverages($"#{channelName} - {channelId}", $"{guildName} - {guildId}", $"{username} @ {userId}", commandAlias);
            _counters.RequestCountN++;
            SystemLogger.Singleton.Verbose(
                "Try execute the command '{0}' with the arguments '{1}' from '{2}' ({3}) in guild '{4}' ({5}) - channel '{6}' ({7}).",
                commandAlias,
                messageContent.Length > 0 ? messageContent.Join(' ').EscapeNewLines().Escape() : "No command arguments.",
                username,
                userId,
                guildName,
                guildId,
                channelName,
                channelId
            );

            await message.Author.FireEventAsync(
                "CommandExecuted",
                string.Format(
                    "Tried to execute the command '{0}' with the arguments '{1}' in guild '{2}' ({3}) - channel '{4}' ({5}).",
                    commandAlias,
                    messageContent.Length > 0 ? messageContent.Join(' ').EscapeNewLines().Escape() : "No command arguments.",
                    guildName,
                    guildId,
                    channelName,
                    channelId
                )
            );

            if (commandAlias.IsNullOrWhiteSpace())
            {
                SystemLogger.Singleton.Warning("We got a prefix in the message, but the command was 0 in length.");
                await message.Author.FireEventAsync("InvalidCommand", "Got prefix but command alias was empty");
                return;
            }

            if (
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.CommandRegistryOnlyMatchAlphabetCharactersForCommandName &&
                !Regex.IsMatch(commandAlias, @"^[a-zA-Z-]*$")
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
                if (!wasRegistered) RegisterOnce();

                var command = GetCommandByCommandAlias(commandAlias);

                if (command == null)
                {
                    await message.Author.FireEventAsync("CommandNotFound", $"{channelId} {commandAlias}");
                    _instrumentationPerfmon.CommandsThatDidNotExist.Increment();
                    _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
                    _counters.RequestFailedCountN++;
                    SystemLogger.Singleton.Warning("The command '{0}' did not exist.", commandAlias);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        _instrumentationPerfmon.NotFoundCommandsThatToldTheFrontendUser.Increment();
                        await message.ReplyAsync($"The command with the name '{commandAlias}' was not found.");
                    }
                    _instrumentationPerfmon.NotFoundCommandsThatDidNotTellTheFrontendUser.Increment();
                    return;
                }

                _instrumentationPerfmon.CommandsThatExist.Increment();

                if (!command.IsEnabled)
                {
                    var disabledMessage = (from cmd in _disabledCommandsReasons where cmd.Item1 == command select cmd.Item2).FirstOrDefault();

                    await message.Author.FireEventAsync("CommandDisabled", $"{channelId} {commandAlias} {disabledMessage ?? ""}");
                    _instrumentationPerfmon.CommandsThatAreDisabled.Increment();
                    SystemLogger.Singleton.Warning("The command '{0}' is disabled. {1}", commandAlias, disabledMessage != null ? $"Because: '{disabledMessage}'" : "");
                    bool isAllowed = false;
                    if (message.Author.IsAdmin())
                    {
                        if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowAdminsToBypassDisabledCommands)
                        {
                            _instrumentationPerfmon.DisabledCommandsThatAllowedAdminBypass.Increment();
                            isAllowed = true;
                        }
                        else
                        {
                            _instrumentationPerfmon.DisabledCommandsThatDidNotAllowAdminBypass.Increment();
                            isAllowed = false;
                        }
                    }

                    if (!isAllowed)
                    {
                        _instrumentationPerfmon.DisabledCommandsThatDidNotAllowBypass.Increment();
                        _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
                        _counters.RequestFailedCountN++;
                        await message.ReplyAsync($"The command by the nameof '{commandAlias}' is disabled, please try again later. {(disabledMessage != null ? $"\nReason: '{disabledMessage}'" : "")}");
                        return;
                    }
                    _instrumentationPerfmon.DisabledCommandsThatWereInvokedToTheFrontendUser.Increment();
                }
                else
                {
                    _instrumentationPerfmon.CommandsThatAreEnabled.Increment();
                }

                _instrumentationPerfmon.CommandsThatPassedAllChecks.Increment();

                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.ExecuteCommandsInNewThread)
                {
                    _instrumentationPerfmon.CommandsThatTryToExecuteInNewThread.Increment();

                    var isAllowed = true;

                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.NewThreadsOnlyAvailableForAdmins)
                    {
                        _instrumentationPerfmon.NewThreadCommandsThatAreOnlyAvailableToAdmins.Increment();
                        if (!message.Author.IsAdmin())
                        {
                            _instrumentationPerfmon.NewThreadCommandsThatDidNotPassAdministratorCheck.Increment();
                            isAllowed = false;
                        }
                        else
                        {
                            _instrumentationPerfmon.NewThreadCommandsThatPassedAdministratorCheck.Increment();
                            isAllowed = true;
                        }
                    }

                    if (isAllowed)
                    {
                        inNewThread = true;
                        _instrumentationPerfmon.NewThreadCommandsThatWereAllowedToExecute.Increment();
                        _instrumentationPerfmon.NewThreadCountersPerSecond.Increment();
                        ExecuteCommandInNewThread(commandAlias, messageContent, message, sw, command);
                        return;
                    }

                    _instrumentationPerfmon.NewThreadCommandsThatWereNotAllowedToExecute.Increment();
                }
                else
                {
                    _instrumentationPerfmon.CommandsThatDidNotTryNewThreadExecution.Increment();
                }

                _instrumentationPerfmon.CommandsNotExecutedInNewThread.Increment();

                await command.Invoke(messageContent, message, commandAlias);

                _instrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                _counters.RequestSucceededCountN++;
            }
            catch (Exception ex)
            {
                await HandleException(ex, commandAlias, message);
            }
            finally
            {
                sw.Stop();
                _instrumentationPerfmon.AverageRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                _instrumentationPerfmon.CommandsThatFinished.Increment();
                SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'{2}.", sw.Elapsed.TotalSeconds, commandAlias, inNewThread ? " in new thread" : "");
            }

            return;
        }

        private void ExecuteSlashCommandInNewThread(string alias, SocketSlashCommand command, IStateSpecificSlashCommandHandler handler, Stopwatch sw)
        {
            SystemLogger.Singleton.LifecycleEvent("Queueing user work item for slash command '{0}'.", alias);

            ThreadPool.QueueUserWorkItem(async s =>
            {
                try
                {
                    _instrumentationPerfmon.NewThreadCommandsThatPassedChecks.Increment();
                    // We do not expect a result here.
                    await handler.Invoke(command);
                    _instrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                    _counters.RequestSucceededCountN++;
                }
                catch (Exception ex)
                {
                    await HandleSlashCommandException(ex, alias, command);
                }
                finally
                {
                    sw.Stop();
                    _instrumentationPerfmon.AverageThreadRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                    _instrumentationPerfmon.NewThreadCommandsThatFinished.Increment();
                    SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'.", sw.Elapsed.TotalSeconds, alias);
                }

            });
        }

        private void ExecuteCommandInNewThread(string alias, string[] messageContent, SocketMessage message, Stopwatch sw, IStateSpecificCommandHandler command)
        {
            SystemLogger.Singleton.LifecycleEvent("Queueing user work item for command '{0}'.", alias);

            // could we have 2 versions here where we pool it and background it?

            ThreadPool.QueueUserWorkItem(async s =>
            {
                try
                {
                    _instrumentationPerfmon.NewThreadCommandsThatPassedChecks.Increment();
                    // We do not expect a result here.
                    await command.Invoke(messageContent, message, alias);
                    _instrumentationPerfmon.SucceededCommandsPerSecond.Increment();
                    _counters.RequestSucceededCountN++;
                }
                catch (Exception ex)
                {
                    await HandleException(ex, alias, message);
                }
                finally
                {
                    sw.Stop();
                    _instrumentationPerfmon.AverageThreadRequestTime.Sample(sw.Elapsed.TotalMilliseconds);
                    _instrumentationPerfmon.NewThreadCommandsThatFinished.Increment();
                    SystemLogger.Singleton.Debug("Took {0}s to execute command '{1}'.", sw.Elapsed.TotalSeconds, alias);
                }
            });
        }

        private async Task HandleSlashCommandException(Exception ex, string alias, SocketSlashCommand command)
        {
            _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
            _counters.RequestFailedCountN++;
            await command.User.FireEventAsync("SlashCommandException", $"The command {alias} threw: {ex.ToDetailedString()}");

            var exceptionID = Guid.NewGuid();

            if (ex is NotSupportedException)
            {
                SystemLogger.Singleton.Warning("This could have been a thread pool error, we'll assume that.");
                return;
            }

            if (ex is ApplicationException)
            {
                SystemLogger.Singleton.Warning("Application threw an exception {0}", ex.ToDetailedString());
                await command.RespondEphemeralAsync($"The command threw an exception: {ex.Message}");
                return;
            }

            if (ex is TimeoutException)
            {
                _instrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                SystemLogger.Singleton.Error("The command '{0}' timed out. {1}", alias, ex.Message);
                await command.RespondEphemeralAsync("the command you tried to execute has timed out, please try identify the leading cause of a timeout.");
                return;
            }

            if (ex is EndpointNotFoundException)
            {
                _instrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                _instrumentationPerfmon.FailedCommandsThatTriedToAccessOfflineGridServer.Increment();
                SystemLogger.Singleton.Warning("The grid service was not online.");
                await command.RespondEphemeralAsync($"the grid service is not currently running, please ask <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}> to start the service.");
                return;
            }

            if (ex is FaultException fault)
            {
                _instrumentationPerfmon.FailedCommandsThatTriggeredAFaultException.Increment();
                SystemLogger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                if (fault.Message == "Cannot invoke BatchJob while another job is running")
                {
                    _instrumentationPerfmon.FailedFaultCommandsThatWereDuplicateInvocations.Increment();
                    await command.RespondEphemeralAsync("You are sending requests too fast, please slow down!");
                    return;
                }

                if (fault.Message == "BatchJob Timeout")
                {
                    _instrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    await command.RespondEphemeralAsync("The job timed out, please try again later.");
                    return;
                }

                _instrumentationPerfmon.FailedFaultCommandsThatWereNotDuplicateInvocations.Increment();
                _instrumentationPerfmon.FailedFaultCommandsThatWereLuaExceptions.Increment();

                if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await command.RespondWithFileEphemeralAsync(new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)), "fault.txt", "An exception occurred on the grid server, please review this error to see if your input was malformed:");
                    return;
                }

                await command.RespondEphemeralAsync(
                    "An exception occurred on the grid server, please review this error to see if your input was malformed:",
                    embed: new EmbedBuilder()
                    .WithColor(0xff, 0x00, 0x00)
                    .WithTitle("GridServer exception.")
                    .WithAuthor(command.User)
                    .WithDescription($"```\n{fault.Message}\n```")
                    .Build()
                );
                return;
            }

            _instrumentationPerfmon.FailedCommandsThatWereUnknownExceptions.Increment();

            SystemLogger.Singleton.Error("[EID-{0}] An unexpected error occurred: {1}", exceptionID.ToString(), ex.ToDetailedString());

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await command.RespondWithFileEphemeralAsync(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt");
                    return;
                }

                _instrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await command.RespondEphemeralAsync(
                    "An error occured with the script execution task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }

            _instrumentationPerfmon.FailedCommandsThatWerePublicallyMasked.Increment();

            await command.RespondEphemeralAsync($"An unexpected Exception has occurred. Exception ID: {exceptionID}, send this ID to <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}>");
            return;
        }

        private async Task HandleException(Exception ex, string alias, SocketMessage message)
        {
            _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
            _counters.RequestFailedCountN++;
            await message.Author.FireEventAsync("CommandException", $"The command {alias} threw: {ex.ToDetailedString()}");

            var exceptionID = Guid.NewGuid();

            if (ex is NotSupportedException)
            {
                SystemLogger.Singleton.Warning("This could have been a thread pool error, we'll assume that.");
                return;
            }

            if (ex is ApplicationException)
            {
                SystemLogger.Singleton.Warning("Application threw an exception {0}", ex.ToDetailedString());
                await message.ReplyAsync($"The command threw an exception: {ex.Message}");
                return;
            }

            if (ex is TimeoutException)
            {
                _instrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                SystemLogger.Singleton.Error("The command '{0}' timed out. {1}", alias, ex.Message);
                await message.ReplyAsync("the command you tried to execute has timed out, please try identify the leading cause of a timeout.");
                return;
            }

            if (ex is EndpointNotFoundException)
            {
                _instrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                _instrumentationPerfmon.FailedCommandsThatTriedToAccessOfflineGridServer.Increment();
                SystemLogger.Singleton.Warning("The grid service was not online.");
                await message.ReplyAsync($"the grid service is not currently running, please ask <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}> to start the service.");
                return;
            }

            if (ex is FaultException fault)
            {
                _instrumentationPerfmon.FailedCommandsThatTriggeredAFaultException.Increment();
                SystemLogger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                if (fault.Message == "Cannot invoke BatchJob while another job is running")
                {
                    _instrumentationPerfmon.FailedFaultCommandsThatWereDuplicateInvocations.Increment();
                    await message.ReplyAsync("You are sending requests too fast, please slow down!");
                    return;
                }

                if (fault.Message == "BatchJob Timeout")
                {
                    _instrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    await message.ReplyAsync("The job timed out, please try again later.");
                    return;
                }

                _instrumentationPerfmon.FailedFaultCommandsThatWereNotDuplicateInvocations.Increment();
                _instrumentationPerfmon.FailedFaultCommandsThatWereLuaExceptions.Increment();

                if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await message.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)), "fault.txt", "An exception occurred on the grid server, please review this error to see if your input was malformed:");
                    return;
                }

                await message.Channel.SendMessageAsync(
                    "An exception occurred on the grid server, please review this error to see if your input was malformed:",
                    embed: new EmbedBuilder()
                    .WithColor(0xff, 0x00, 0x00)
                    .WithTitle("GridServer exception.")
                    .WithAuthor(message.Author)
                    .WithDescription($"```\n{fault.Message}\n```")
                    .Build()
                );
                return;
            }

            _instrumentationPerfmon.FailedCommandsThatWereUnknownExceptions.Increment();

            SystemLogger.Singleton.Error("[EID-{0}] An unexpected error occurred: {1}", exceptionID.ToString(), ex.ToDetailedString());

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await message.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt");
                    return;
                }

                _instrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await message.ReplyAsync(
                    "An error occured with the script execution task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }

            _instrumentationPerfmon.FailedCommandsThatWerePublicallyMasked.Increment();

            await message.ReplyAsync($"An unexpected Exception has occurred. Exception ID: {exceptionID}, send this ID to <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}>");
            return;
        }

        public IStateSpecificCommandHandler GetCommandByCommandAlias(string alias)
        {
            lock (_stateSpecificCommandHandlers)
                return (from command in _stateSpecificCommandHandlers where command.CommandAliases.Contains(alias) select command).FirstOrDefault();
        }

        public IStateSpecificSlashCommandHandler GetSlashCommandByCommandAlias(string alias)
        {
            lock (_stateSpecificCommandHandlers)
                return (from command in _stateSpecificSlashCommandHandlers where command.CommandAlias == alias select command).FirstOrDefault();
        }

        private void ParseAndInsertIntoCommandRegistry()
        {
            lock (_stateSpecificCommandHandlers)
            {
                _instrumentationPerfmon.CommandsParsedAndInsertedIntoRegistry.Increment();
                SystemLogger.Singleton.LifecycleEvent("Begin attempt to register commands via Reflection");

                try
                {
                    var defaultCommandNamespace = GetDefaultCommandNamespace();
                    var slashCommandNamespace = GetSlashCommandNamespace();

                    SystemLogger.Singleton.Info("Got default command namespace '{0}'.", defaultCommandNamespace);
                    SystemLogger.Singleton.Info("Got slash command namespace '{0}'.", slashCommandNamespace);

                    var defaultCommandTypes = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(defaultCommandNamespace);
                    var slashCommandTypes = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(slashCommandNamespace);

                    if (slashCommandTypes.Length == 0)
                    {
                        _instrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                        SystemLogger.Singleton.Warning("There were no slash commands found in the namespace '{0}'.", slashCommandNamespace);
                    }
                    else
                    {
                        foreach (var type in slashCommandTypes)
                        {
                            if (type.IsClass)
                            {
                                var commandHandler = Activator.CreateInstance(type);
                                if (commandHandler is IStateSpecificSlashCommandHandler trueCommandHandler)
                                {
                                    SystemLogger.Singleton.Info("Parsing slash command '{0}'.", type.FullName);

                                    if (trueCommandHandler.CommandAlias.IsNullOrEmpty())
                                    {
                                        _instrumentationPerfmon.StateSpecificCommandsThatHadNoAliases.Increment();
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Expected the sizeof field 'CommandAlias' to not be null or empty",
                                            type.FullName
                                        );

                                        continue;
                                    }
                                    if (trueCommandHandler.CommandName.IsNullOrEmpty())
                                    {
                                        _instrumentationPerfmon.StateSpecificCommandsThatHadNoName.Increment();
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Expected field 'CommandName' to be not null",
                                            type.FullName
                                        );

                                        continue;
                                    }

                                    if (trueCommandHandler.CommandDescription != null)
                                    {
                                        if (trueCommandHandler.CommandDescription.Length == 0)
                                        {
                                            _instrumentationPerfmon.StateSpecificCommandsThatHadNoNullButEmptyDescription.Increment();
                                            SystemLogger.Singleton.Warning(
                                                "Exception when reading '{0}': Expected field 'CommandDescription' to have a size greater than 0",
                                                type.FullName
                                            );
                                        }
                                    }

                                    _instrumentationPerfmon.StateSpecificCommandsThatWereAddedToTheRegistry.Increment();

                                    var builder = new SlashCommandBuilder();

                                    builder.WithName(trueCommandHandler.CommandAlias);
                                    builder.WithDescription($"{trueCommandHandler.CommandName} ({trueCommandHandler.CommandAlias}): {trueCommandHandler.CommandDescription}");
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

                                    _stateSpecificSlashCommandHandlers.Add(trueCommandHandler);
                                }
                            }
                            else
                            {
                                _instrumentationPerfmon.CommandsInNamespaceThatWereNotClasses.Increment();
                            }
                        }
                    }

                    if (defaultCommandTypes.Length == 0)
                    {
                        _instrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                        SystemLogger.Singleton.Warning("There were no default commands found in the namespace '{0}'.", defaultCommandNamespace);
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
                                        _instrumentationPerfmon.StateSpecificCommandsThatHadNoAliases.Increment();
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Expected the sizeof field 'CommandAliases' to be greater than 0, got {1}",
                                            type.FullName,
                                            trueCommandHandler.CommandAliases.Length
                                        );

                                        continue;
                                    }

                                    if (trueCommandHandler.CommandName.IsNullOrEmpty())
                                    {
                                        _instrumentationPerfmon.StateSpecificCommandsThatHadNoName.Increment();
                                        SystemLogger.Singleton.Trace(
                                            "Exception when reading '{0}': Expected field 'CommandName' to be not null",
                                            type.FullName
                                        );

                                        continue;
                                    }

                                    if (trueCommandHandler.CommandDescription != null)
                                    {
                                        if (trueCommandHandler.CommandDescription.Length == 0)
                                        {
                                            _instrumentationPerfmon.StateSpecificCommandsThatHadNoNullButEmptyDescription.Increment();
                                            SystemLogger.Singleton.Warning(
                                                "Exception when reading '{0}': Expected field 'CommandDescription' to have a size greater than 0",
                                                type.FullName
                                            );
                                        }
                                    }

                                    _instrumentationPerfmon.StateSpecificCommandsThatWereAddedToTheRegistry.Increment();



                                    _stateSpecificCommandHandlers.Add(trueCommandHandler);
                                }
                                else
                                {
                                    _instrumentationPerfmon.CommandThatWereNotStateSpecific.Increment();
                                }
                            }
                            else
                            {
                                _instrumentationPerfmon.CommandsInNamespaceThatWereNotClasses.Increment();
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    _instrumentationPerfmon.CommandRegistryRegistrationsThatFailed.Increment();
                    SystemLogger.Singleton.Error(ex);
                }
                finally
                {
                    SystemLogger.Singleton.Verbose("Successfully initialized the CommandRegistry.");
                }
            }
        }



        public void RegisterOnce()
        {
            if (!wasRegistered)
            {
                lock (_registrationLock)
                {
                    ParseAndInsertIntoCommandRegistry();
                    wasRegistered = true;
                }
            }
        }

        #region Legacy Metrics

        public void LogMetricsReport()
        {
            SystemLogger.Singleton.Warning(
                "Command Registry metrics report for Date ({0} at {1})",
                DateTimeGlobal.Singleton.GetUtcNowAsISO(),
                LoggingSystem.Singleton.GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("f7")
            );
            SystemLogger.Singleton.Log("=====================================================================================");
            SystemLogger.Singleton.Log("Total command request count: {0}", _counters.RequestCountN);
            SystemLogger.Singleton.Log("Total succeeded command request count: {0}", _counters.RequestSucceededCountN);
            SystemLogger.Singleton.Log("Total failed command request count: {0}", _counters.RequestFailedCountN);

            var modes = CalculateModes();

            SystemLogger.Singleton.Log("Average request channel: '{0}' with average of {1}", modes.Channels.item, modes.Channels.average);
            SystemLogger.Singleton.Log("Average request guild: '{0}' with average of {1}", modes.Servers.item, modes.Servers.average);
            SystemLogger.Singleton.Log("Average request user: '{0}' with average of {1}", modes.Users.item, modes.Users.average);
            SystemLogger.Singleton.Log("Average request command name: '{0}' with average of {1}", modes.Commands.item, modes.Commands.average);

            SystemLogger.Singleton.Log("=====================================================================================");
        }

        private void InsertIntoAverages(string channelName, string serverName, string userName, string commandName)
        {
            _averages.Channels.Add(channelName);
            _averages.Servers.Add(serverName);
            _averages.Users.Add(userName);
            _averages.Commands.Add(commandName);
        }

        private Modes CalculateModes()
        {
            return new Modes
            {
                Channels = CalculateModeOfArray(_averages.Channels),
                Servers = CalculateModeOfArray(_averages.Servers),
                Users = CalculateModeOfArray(_averages.Users),
                Commands = CalculateModeOfArray(_averages.Commands)
            };
        }

        private readonly Counters _counters = new Counters();
        private readonly Averages _averages = new Averages();

        private class Averages
        {
            internal ICollection<string> Channels = new List<string>();
            internal ICollection<string> Servers = new List<string>();
            internal ICollection<string> Users = new List<string>();
            internal ICollection<string> Commands = new List<string>();
        }

        private class Counters
        {
            internal int RequestCountN = 0;
            internal int RequestFailedCountN = 0;
            internal int RequestSucceededCountN = 0;
        }

        private class Modes
        {
            internal Mode<string> Channels;
            internal Mode<string> Servers;
            internal Mode<string> Users;
            internal Mode<string> Commands;
        }

        private Mode<T> CalculateModeOfArray<T>(ICollection<T> collection)
        {
            try
            {
                var array = collection.ToArray<T>();

                if (array == null || array.Length == 0)
                    return new Mode<T>()
                    {
                        item = default,
                        average = 0
                    };

                var mf = 1;
                var m = 0;
                T item = default;
                for (var i = 0; i < array.Length; i++)
                {
                    for (var j = i; j < array.Length; j++)
                    {
                        if (array[i].Equals(array[j])) m++;
                        if (mf < m)
                        {
                            mf = m;
                            item = array[i];
                        }
                    }
                    m = 0;
                }

                return new Mode<T>()
                {
                    item = EqualityComparer<T>.Default.Equals(item, default) ? array[array.Length - 1] : item,
                    average = mf
                };
            }
            catch
            {
                return new Mode<T>()
                {
                    item = default,
                    average = 0
                };
            }
        }

        private class Mode<T>
        {
            internal T item;
            internal int average;
        }

        #endregion Legacy Metrics
    }
}
