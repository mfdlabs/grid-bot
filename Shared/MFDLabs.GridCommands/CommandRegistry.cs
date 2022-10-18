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
using MFDLabs.Threading;

#if WE_LOVE_EM_SLASH_COMMANDS
using MFDLabs.Grid.Bot.Global;
#endif // WE_LOVE_EM_SLASH_COMMANDS

// ReSharper disable AsyncVoidLambda

namespace MFDLabs.Grid.Bot.Registries
{
    public static class CommandRegistry
    {
        private const string UnhandledExceptionOccurredFromCommand = "An error occured with the command and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:";

        private static bool _wasRegistered;
        private static readonly object RegistrationLock = new();

        private static readonly ICollection<CommandCircuitBreakerWrapper> CommandCircuitBreakerWrappers = new List<CommandCircuitBreakerWrapper>();
        private static readonly ICollection<IStateSpecificCommandHandler> StateSpecificCommandHandlers = new List<IStateSpecificCommandHandler>();
        private static ICollection<(IStateSpecificCommandHandler, string)> _disabledCommandsReasons = new List<(IStateSpecificCommandHandler, string)>();

#if WE_LOVE_EM_SLASH_COMMANDS

        private static readonly ICollection<SlashCommandCircuitBreakerWrapper> SlashCommandCircuitBreakerWrappers = new List<SlashCommandCircuitBreakerWrapper>();
        private static readonly ICollection<IStateSpecificSlashCommandHandler> StateSpecificSlashCommandHandlers = new List<IStateSpecificSlashCommandHandler>();
        private static ICollection<(IStateSpecificSlashCommandHandler, string)> _disabledSlashCommandsReasons = new List<(IStateSpecificSlashCommandHandler, string)>();

#endif // WE_LOVE_EM_SLASH_COMMANDS

        private static readonly CommandRegistryInstrumentationPerformanceMonitor InstrumentationPerfmon = new(PerfmonCounterRegistryProvider.Registry);

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

        public static Embed ConstructHelpEmbedForSingleSlashCommand(string commandName, IUser author)
        {
            if (!_wasRegistered) RegisterOnce();

            var command = GetSlashCommandByCommandAlias(commandName);

            if (command == default) return null;

            var isInternal = command.Internal;
            var isDisabled = command.IsEnabled == false;

            if (isInternal && !author.IsAdmin()) return null;

            var builder = new EmbedBuilder
            {
                Title = $"{command.CommandAlias} Documentation",
                Color = isDisabled ? new Color(0xff, 0x00, 0x00) : new Color(0x00, 0xff, 0x00)
            };
            builder.AddField(
                "Description",
                $"{command.CommandDescription}\n{(isInternal ? ":no_entry:" : "")} {(isDisabled ? ":x:" : ":white_check_mark:")}"
            );
            builder.WithCurrentTimestamp();
            builder.Description = ":no_entry:\\: **INTERNAL**\n:x:\\: **DISABLED**\n:white_check_mark:\\: **ENABLED**";

            return builder.Build();
        }

        public static ICollection<Embed> ConstructHelpEmbedForAllSlashCommands(IUser author)
        {
            if (!_wasRegistered) RegisterOnce();

            var builder = new EmbedBuilder().WithTitle("Documentation");
            var embeds = new List<Embed>();
            var i = 0;

            foreach (var command in StateSpecificSlashCommandHandlers)
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
                    "Description",
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

#endif // WE_LOVE_EM_SLASH_COMMANDS

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
            var subCommand = command.Data.GetSubCommand();
            var args = (from opt in subCommand != null ? subCommand.Options : command.Data.Options select $"{opt.Name} = {opt.Value}").Join(", ");
            var subCommandName = subCommand != null ? subCommand.Name + " " : "";

            InsertIntoAverages($"#{channelName} - {channelId}", $"{guildName} - {guildId}", $"{username} @ {userId}", $"Slash Command - {commandAlias}");
            Counters.RequestCountN++;
            Logger.Singleton.Verbose(
                "Try execute the slash command '{0}' with the arguments '{1}' from '{2}' ({3}) in guild '{4}' ({5}) - channel '{6}' ({7}).",
                commandAlias,
                !args.IsNullOrEmpty()
                    ? $"{subCommandName}{args.EscapeNewLines().Escape()}"
                    : "no arguments.",
                username,
                userId,
                guildName,
                guildId,
                channelName,
                channelId
            );

            await command.User.PageViewedAsync($"{typeof(CommandRegistry).FullName}SlashCommands({channelName})");

            await command.User.FireEventAsync(
                "SlashCommandExecuted",
                $"Try execute the slash command '{commandAlias}' with the arguments '{(!args.IsNullOrEmpty() ? args.EscapeNewLines().Escape() : "No command arguments.")}' " +
                $"from '{username}' ({userId}) in guild '{guildName}' ({guildId}) - channel '{channelName}' ({channelId})."
            );

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

                    Logger.Singleton.Warning("The slash command '{0}' did not exist.", commandAlias);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.IsAllowedToEchoBackNotFoundCommandException)
                    {
                        InstrumentationPerfmon.NotFoundCommandsThatToldTheFrontendUser.Increment();
                        await command.RespondEphemeralPingAsync($"The slash command with the name '{commandAlias}' was not found.");
                    }
                    DeleteSocketCommand(command);
                    InstrumentationPerfmon.NotFoundCommandsThatDidNotTellTheFrontendUser.Increment();
                    return;
                }

                InstrumentationPerfmon.CommandsThatExist.Increment();

                if (!cmd.IsEnabled)
                {
                    var disabledMessage = (from c in _disabledSlashCommandsReasons select c.Item2).FirstOrDefault();

                    await command.User.FireEventAsync("SlashCommandDisabled", $"{channelId} {commandAlias} {disabledMessage ?? ""}");
                    InstrumentationPerfmon.CommandsThatAreDisabled.Increment();
                    Logger.Singleton.Warning("The slash command '{0}' is disabled. {1}",
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
                        await command.RespondEphemeralPingAsync($"The command by the name of '{commandAlias}' is disabled, please try again later. {(disabledMessage != null ? $"\nReason: '{disabledMessage}'" : "")}");
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

                await GetSlashCommandWrapperByCommand(cmd).ExecuteAsync(command);

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
                Logger.Singleton.Debug(
                    "Took {0}s to execute command '{1}'{2}.",
                    sw.Elapsed.TotalSeconds,
                    commandAlias,
                    inNewThread ? " in new thread" : ""
                );
            }
        }

        private static void DeleteSocketCommand(SocketSlashCommand cmd)
        {
            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    SocketApplicationCommand cmdInternal = null;

                    if (cmd.Channel is SocketGuildChannel guildChannel)
                    {
                        cmdInternal = await guildChannel.Guild.GetApplicationCommandAsync(cmd.CommandId);
                    }

                    if (cmdInternal == null)
                    {
                        cmdInternal = await BotGlobal.Client.GetGlobalApplicationCommandAsync(cmd.CommandId);
                    }

                    await cmdInternal.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Logger.Singleton.Error(ex);
                }
            });
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
            Logger.Singleton.Verbose(
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

            await message.Author.PageViewedAsync($"{typeof(CommandRegistry).FullName}({channelName})");

            await message.Author.FireEventAsync(
                "CommandExecuted",
                $"Tried to execute the command '{commandAlias}' with the arguments " +
                $"'{(messageContent.Length > 0 ? messageContent.Join(' ').EscapeNewLines().Escape() : "No command arguments.")}' " +
                $"in guild '{guildName}' ({guildId}) - channel '{channelName}' ({channelId})."
            );

            if (commandAlias.IsNullOrWhiteSpace())
            {
                Logger.Singleton.Warning("We got a prefix in the message, but the command was 0 in length.");
                await message.Author.FireEventAsync("InvalidCommand", "Got prefix but command alias was empty");
                return;
            }

            if (
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.CommandRegistryOnlyMatchAlphabetCharactersForCommandName &&
                !Regex.IsMatch(commandAlias, @"^[a-zA-Z-]*$") &&
                !message.Author.IsAdmin()
            )
            {
                Logger.Singleton.Warning("We got a prefix in the message, but the command contained non-alphabetic characters, message: {0}", commandAlias);
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
                    Logger.Singleton.Warning("The command '{0}' did not exist.", commandAlias);
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
                    Logger.Singleton.Warning("The command '{0}' is disabled. {1}",
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
                Logger.Singleton.Debug("Took {0}s to execute command '{1}'{2}.",
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
            Logger.Singleton.LifecycleEvent("Queueing user work item for slash command '{0}'.", alias);

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    InstrumentationPerfmon.NewThreadCommandsThatPassedChecks.Increment();
                    // We do not expect a result here.
                    await GetSlashCommandWrapperByCommand(handler).ExecuteAsync(command);
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
                    Logger.Singleton.Debug("Took {0}s to execute command '{1}'.", sw.Elapsed.TotalSeconds, alias);
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
            Logger.Singleton.LifecycleEvent("Queueing user work item for command '{0}'.", alias);

            // could we have 2 versions here where we pool it and background it?

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                try
                {
                    InstrumentationPerfmon.NewThreadCommandsThatPassedChecks.Increment();
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
                    Logger.Singleton.Debug("Took {0}s to execute command '{1}'.", sw.Elapsed.TotalSeconds, alias);
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
                    Logger.Singleton.Warning("This could have been a thread pool error, we'll assume that.");
                    return;
                case ApplicationException _:
                    Logger.Singleton.Warning("Application threw an exception {0}", ex.ToDetailedString());
                    await command.RespondEphemeralPingAsync($"The command threw an exception: {ex.Message}");
                    return;
                case TimeoutException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    Logger.Singleton.Error("The command '{0}' timed out. {1}", alias, ex.Message);
                    await command.RespondEphemeralPingAsync("the command you tried to execute has timed out, please " +
                                                        "try identify the leading cause of a timeout.");
                    return;
                case EndpointNotFoundException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    InstrumentationPerfmon.FailedCommandsThatTriedToAccessOfflineGridServer.Increment();
                    Logger.Singleton.Warning("The grid service was not online.");
                    await command.RespondEphemeralPingAsync($"the grid service is not currently running, " +
                                                        $"please ask <@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}>" +
                                                        $" to start the service.");
                    return;
                case FaultException fault:
                {
                    InstrumentationPerfmon.FailedCommandsThatTriggeredAFaultException.Increment();
                    Logger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

                    if (fault.Message == "Cannot invoke BatchJob while another job is running")
                    {
                        InstrumentationPerfmon.FailedFaultCommandsThatWereDuplicateInvocations.Increment();
                        await command.RespondEphemeralPingAsync("You are sending requests too fast, please slow down!");
                        return;
                    }

                    if (fault.Message == "BatchJob Timeout")
                    {
                        InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                        await command.RespondEphemeralPingAsync("The job timed out, please try again later.");
                        return;
                    }

                    InstrumentationPerfmon.FailedFaultCommandsThatWereNotDuplicateInvocations.Increment();
                    InstrumentationPerfmon.FailedFaultCommandsThatWereLuaExceptions.Increment();

                    if (fault.Message.Length > EmbedBuilder.MaxDescriptionLength)
                    {
                        await command.RespondEphemeralPingAsync("An exception occurred on the grid server, please review this error to see if your input was malformed:");
                        await command.RespondWithFileEphemeralPingAsync(new MemoryStream(Encoding.UTF8.GetBytes(fault.Message)), "fault.txt", "Fault From Server:");
                        return;
                    }

                    await command.RespondEphemeralPingAsync(
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
            }

            InstrumentationPerfmon.FailedCommandsThatWereUnknownExceptions.Increment();

            Logger.Singleton.Error("[EID-{0}] An unexpected error occurred: {1}", exceptionId.ToString(), ex.ToDetailedString());

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await command.RespondEphemeralPingAsync(UnhandledExceptionOccurredFromCommand);
                    await command.RespondWithFileEphemeralPingAsync(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt", "Exception From Command:");
                    return;
                }

                InstrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await command.RespondEphemeralPingAsync(
                    UnhandledExceptionOccurredFromCommand,
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }

            InstrumentationPerfmon.FailedCommandsThatWerePublicallyMasked.Increment();

            await command.RespondEphemeralPingAsync(
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
                global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);

            switch (ex)
            {
                case CircuitBreakerException _:
                    Logger.Singleton.Warning("CircuitBreakerException '{0}'", ex.ToDetailedString());
                    await message.ReplyAsync(ex.Message);
                    return;
                case NotSupportedException _:
                    Logger.Singleton.Warning("This could have been a thread pool error, we'll assume that.");
                    return;
                case ApplicationException _:
                    Logger.Singleton.Warning("Application threw an exception {0}", ex.ToDetailedString());
                    await message.ReplyAsync($"The command threw an exception: {ex.Message}");
                    return;
                case TimeoutException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    Logger.Singleton.Error("The command '{0}' timed out. {1}", alias, ex.Message);
                    await message.ReplyAsync("the command you tried to execute has timed out, please try identify " +
                                             "the leading cause of a timeout.");
                    return;
                case EndpointNotFoundException _:
                    InstrumentationPerfmon.FailedCommandsThatTimedOut.Increment();
                    InstrumentationPerfmon.FailedCommandsThatTriedToAccessOfflineGridServer.Increment();
                    Logger.Singleton.Warning("The grid service was not online.");
                    await message.ReplyAsync($"the grid service is not currently running, please ask " +
                                             $"<@!{MFDLabs.Grid.Bot.Properties.Settings.Default.BotOwnerID}> to start " +
                                             $"the service.");
                    return;
                case FaultException fault:
                {
                    InstrumentationPerfmon.FailedCommandsThatTriggeredAFaultException.Increment();
                    Logger.Singleton.Warning("An error occured on the grid server: {0}", fault.Message);

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

            Logger.Singleton.Error("[EID-{0}] An unexpected error occurred: {1}",
                exceptionId.ToString(),
                ex.ToDetailedString());

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    await message.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt", UnhandledExceptionOccurredFromCommand);
                    return;
                }

                InstrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await message.ReplyAsync(
                    UnhandledExceptionOccurredFromCommand,
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
            lock (CommandCircuitBreakerWrappers)
                return (from wrapper in CommandCircuitBreakerWrappers
                        where wrapper.Command.CommandName == command.CommandName
                        select wrapper).FirstOrDefault();
        }

#if WE_LOVE_EM_SLASH_COMMANDS

        private static IStateSpecificSlashCommandHandler GetSlashCommandByCommandAlias(string alias)
        {
            lock (StateSpecificSlashCommandHandlers)
                return (from command in StateSpecificSlashCommandHandlers where command.CommandAlias == alias select command).FirstOrDefault();
        }

        private static SlashCommandCircuitBreakerWrapper GetSlashCommandWrapperByCommand(IStateSpecificSlashCommandHandler command)
        {
            lock (SlashCommandCircuitBreakerWrappers)
                return (from wrapper in SlashCommandCircuitBreakerWrappers
                        where wrapper.Command.CommandAlias == command.CommandAlias
                        select wrapper).FirstOrDefault();
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        private static void ParseAndInsertIntoCommandRegistry()
        {
            lock (StateSpecificCommandHandlers)
            {
                InstrumentationPerfmon.CommandsParsedAndInsertedIntoRegistry.Increment();
                Logger.Singleton.LifecycleEvent("Begin attempt to register commands via Reflection");

                try
                {
                    var defaultCommandNamespace = GetDefaultCommandNamespace();

                    Logger.Singleton.Info("Got default command namespace '{0}'.", defaultCommandNamespace);

                    var defaultCommandTypes = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(defaultCommandNamespace);

#if WE_LOVE_EM_SLASH_COMMANDS

                    // Queue up a thread here because slash command
                    // registration can block the main thread
                    ThreadPool.QueueUserWorkItem(s =>
                    {

                        var slashCommandNamespace = GetSlashCommandNamespace();
                        Logger.Singleton.Info("Got slash command namespace '{0}'.", slashCommandNamespace);
                        var slashCommandTypes = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(slashCommandNamespace);


                        if (slashCommandTypes.Length == 0)
                        {
                            InstrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                            Logger.Singleton.Warning("There were no slash commands found in the namespace '{0}'.", slashCommandNamespace);
                        }
                        else
                        {
                            foreach (var type in slashCommandTypes)
                            {
                                try
                                {
                                    if (type.IsClass)
                                    {
                                        var commandHandler = Activator.CreateInstance(type);

                                        if (commandHandler is not IStateSpecificSlashCommandHandler trueCommandHandler) continue;

                                        Logger.Singleton.Info("Parsing slash command '{0}'.", type.FullName);

                                        if (trueCommandHandler.CommandAlias.IsNullOrEmpty())
                                        {
                                            InstrumentationPerfmon.StateSpecificCommandsThatHadNoAliases.Increment();
                                            Logger.Singleton.Error(
                                                "Exception when reading '{0}': Expected the sizeof field 'CommandAlias' " +
                                                "to not be null or empty",
                                                type.FullName
                                            );

                                            continue;
                                        }

                                        if (GetSlashCommandByCommandAlias(trueCommandHandler.CommandAlias) != null)
                                        {
                                            InstrumentationPerfmon.StateSpecificCommandAliasesThatAlreadyExisted.Increment();
                                            Logger.Singleton.Error(
                                                "Exception when reading '{0}': There is already an existing command with the alias of '{1}'",
                                                type.FullName,
                                                trueCommandHandler.CommandAlias
                                            );
                                            continue;
                                        }

                                        if (trueCommandHandler.CommandDescription is { Length: 0 })
                                        {
                                            InstrumentationPerfmon.StateSpecificCommandsThatHadNoNullButEmptyDescription.Increment();
                                            Logger.Singleton.Error(
                                                "Exception when reading '{0}': Expected field 'CommandDescription' " +
                                                "to have a size greater than 0",
                                                type.FullName
                                            );
                                        }

                                        InstrumentationPerfmon.StateSpecificCommandsThatWereAddedToTheRegistry.Increment();

                                        var builder = new SlashCommandBuilder();

                                        builder.WithName(trueCommandHandler.CommandAlias);
                                        builder.WithDescription(trueCommandHandler.CommandDescription);
                                        builder.WithDefaultPermission(true); // command is enabled by default

                                        if (trueCommandHandler.Options != null)
                                            builder.AddOptions(trueCommandHandler.Options);

                                        if (trueCommandHandler.GuildId != null)
                                        {
                                            var guild = BotGlobal.Client.GetGuild(trueCommandHandler.GuildId.Value);

                                            if (guild == null)
                                            {
                                                Logger.Singleton.Trace(
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
                                            BotGlobal.Client.CreateGlobalApplicationCommand(builder.Build());
                                        }

                                        StateSpecificSlashCommandHandlers.Add(trueCommandHandler);
                                        SlashCommandCircuitBreakerWrappers.Add(new SlashCommandCircuitBreakerWrapper(trueCommandHandler));
                                    }
                                    else
                                    {
                                        InstrumentationPerfmon.CommandsInNamespaceThatWereNotClasses.Increment();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    InstrumentationPerfmon.CommandRegistryRegistrationsThatFailed.Increment();
                                    Logger.Singleton.Error(ex);
                                }
                            }
                        }
                    });

#endif // WE_LOVE_EM_SLASH_COMMANDS

                    if (defaultCommandTypes.Length == 0)
                    {
                        InstrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                        Logger.Singleton.Warning("There were no default commands found in the namespace '{0}'.",
                            defaultCommandNamespace);
                    }
                    else
                    {
                        foreach (var type in defaultCommandTypes)
                        {
                            try
                            {
                                if (type.IsClass)
                                {
                                    var commandHandler = Activator.CreateInstance(type);
                                    if (commandHandler is IStateSpecificCommandHandler trueCommandHandler)
                                    {
                                        Logger.Singleton.Info("Parsing command '{0}'.", type.FullName);

                                        if (trueCommandHandler.CommandAliases.Length < 1)
                                        {
                                            InstrumentationPerfmon.StateSpecificCommandsThatHadNoAliases.Increment();
                                            Logger.Singleton.Trace(
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
                                            Logger.Singleton.Trace(
                                                "Exception when reading '{0}': Expected field 'CommandName' to be not null",
                                                type.FullName
                                            );

                                            continue;
                                        }

                                        if (trueCommandHandler.CommandDescription is { Length: 0 })
                                        {
                                            InstrumentationPerfmon.StateSpecificCommandsThatHadNoNullButEmptyDescription.Increment();
                                            Logger.Singleton.Warning(
                                                "Exception when reading '{0}': Expected field " +
                                                "'CommandDescription' to have a size greater than 0",
                                                type.FullName
                                            );
                                        }

                                        InstrumentationPerfmon.StateSpecificCommandsThatWereAddedToTheRegistry.Increment();



                                        StateSpecificCommandHandlers.Add(trueCommandHandler);
                                        CommandCircuitBreakerWrappers.Add(new CommandCircuitBreakerWrapper(trueCommandHandler));
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
                            catch (Exception ex)
                            {
                                InstrumentationPerfmon.CommandRegistryRegistrationsThatFailed.Increment();
                                Logger.Singleton.Error(ex);
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    InstrumentationPerfmon.CommandRegistryRegistrationsThatFailed.Increment();
                    Logger.Singleton.Error(ex);
                }
                finally
                {
                    Logger.Singleton.Verbose("Successfully initialized the CommandRegistry.");
                }
            }
        }

        public static void Unregister()
        {
            if (!_wasRegistered) return;

            lock (RegistrationLock)
            {
                _wasRegistered = false;
                _disabledCommandsReasons.Clear();
                StateSpecificCommandHandlers.Clear();
                CommandCircuitBreakerWrappers.Clear();

#if WE_LOVE_EM_SLASH_COMMANDS

                _disabledSlashCommandsReasons.Clear();
                StateSpecificSlashCommandHandlers.Clear();
                SlashCommandCircuitBreakerWrappers.Clear();

#endif
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
            Logger.Singleton.Warning(
                "Command Registry metrics report for Date ({0} at {1})",
                DateTimeGlobal.GetUtcNowAsIso(),
                LoggingSystem.GlobalLifetimeWatch.Elapsed.TotalSeconds.ToString("f7")
            );
            Logger.Singleton.Log("=====================================================================================");
            Logger.Singleton.Log("Total command request count: {0}", Counters.RequestCountN);
            Logger.Singleton.Log("Total succeeded command request count: {0}", Counters.RequestSucceededCountN);
            Logger.Singleton.Log("Total failed command request count: {0}", Counters.RequestFailedCountN);

            var modes = CalculateModes();

            Logger.Singleton.Log("Average request channel: '{0}' with average of {1}", modes.Channels.Item, modes.Channels.Average);
            Logger.Singleton.Log("Average request guild: '{0}' with average of {1}", modes.Servers.Item, modes.Servers.Average);
            Logger.Singleton.Log("Average request user: '{0}' with average of {1}", modes.Users.Item, modes.Users.Average);
            Logger.Singleton.Log("Average request command name: '{0}' with average of {1}", modes.Commands.Item, modes.Commands.Average);

            Logger.Singleton.Log("=====================================================================================");
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
            public Atomic RequestCountN = 0;
            public Atomic RequestFailedCountN = 0;
            public Atomic RequestSucceededCountN = 0;
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
