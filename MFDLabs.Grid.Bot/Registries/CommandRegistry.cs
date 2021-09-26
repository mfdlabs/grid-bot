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
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Logging.Diagnostics;
using MFDLabs.Networking;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Registries
{
    public sealed class CommandRegistry : SingletonBase<CommandRegistry>
    {
        private bool wasRegistered = false;

        private readonly object _registrationLock = new object();

        private readonly ICollection<IStateSpecificCommandHandler> _stateSpecificCommandHandlers = new List<IStateSpecificCommandHandler>();

        private readonly CommandRegistryInstrumentationPerformanceMonitor _instrumentationPerfmon = new CommandRegistryInstrumentationPerformanceMonitor(StaticCounterRegistry.Instance);

        private string GetCommandNamespace()
        {
            return $"{typeof(Program).Namespace}.Commands";
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

        public bool SetIsEnabled(string commandName, bool isEnabled)
        {
            var command = GetCommandByCommandAlias(commandName.ToLower());

            if (command == null) return false;

            lock (_stateSpecificCommandHandlers)
            {
                _stateSpecificCommandHandlers.Remove(command);

                command.IsEnabled = isEnabled;

                _stateSpecificCommandHandlers.Add(command);
            }

            return true;
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
            var sw = Stopwatch.StartNew();
            var inNewThread = false;

            try
            {
                if (!wasRegistered) RegisterOnce();

                var command = GetCommandByCommandAlias(commandAlias);

                if (command == null)
                {
                    _instrumentationPerfmon.CommandsThatDidNotExist.Increment();
                    _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
                    _counters.RequestFailedCountN++;
                    SystemLogger.Singleton.Warning("The command '{0}' did not exist.", commandAlias);
                    if (Settings.Singleton.IsAllowedToEchoBackNotFoundCommandException)
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
                    _instrumentationPerfmon.CommandsThatAreDisabled.Increment();
                    SystemLogger.Singleton.Warning("The command '{0}' is disabled.", commandAlias);
                    bool isAllowed = false;
                    if (message.Author.IsAdmin())
                    {
                        if (Settings.Singleton.AllowAdminsToBypassDisabledCommands)
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
                        await message.ReplyAsync($"The command by the nameof '{commandAlias}' is disabled, please try again later.");
                        return;
                    }
                    _instrumentationPerfmon.DisabledCommandsThatWereInvokedToTheFrontendUser.Increment();
                }
                else
                {
                    _instrumentationPerfmon.CommandsThatAreEnabled.Increment();
                }

                _instrumentationPerfmon.CommandsThatPassedAllChecks.Increment();

                if (Settings.Singleton.ExecuteCommandsInNewThread)
                {
                    _instrumentationPerfmon.CommandsThatTryToExecuteInNewThread.Increment();

                    var isAllowed = true;

                    if (Settings.Singleton.NewThreadsOnlyAvailableForAdmins)
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
                        _instrumentationPerfmon.NewThreadCommandsThatWereAllowedToExecute.Increment();
                        _instrumentationPerfmon.NewThreadCountersPerSecond.Increment();
                        inNewThread = ExecuteCommandInNewThread(commandAlias, messageContent, message, sw, command);
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

        private bool ExecuteCommandInNewThread(string alias, string[] messageContent, SocketMessage message, Stopwatch sw, IStateSpecificCommandHandler command)
        {
            bool inNewThread;
            var threadName = NetworkingGlobal.Singleton.GenerateUUIDV4();

            SystemLogger.Singleton.LifecycleEvent("Executing command '{0}' in new thread '{1}'.", alias, threadName);

            inNewThread = true;

            new Thread(async () =>
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
            })
            {
                IsBackground = false,
                Name = threadName,
                Priority = ThreadPriority.Normal,
            }.Start();
            return inNewThread;
        }

        private async Task HandleException(Exception ex, string alias, SocketMessage message)
        {
            _instrumentationPerfmon.FailedCommandsPerSecond.Increment();
            _counters.RequestFailedCountN++;

            var exceptionID = Guid.NewGuid();

            if (ex is ApplicationException)
            {
                SystemLogger.Singleton.Warning("Application threw an exception {0}", ex.Message);
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
                await message.ReplyAsync($"the grid service is not currently running, please ask <@!{Settings.Singleton.BotOwnerID}> to start the service.");
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

                await message.ReplyAsync($"an exception occurred on the grid server, please review this error to see if your input was malformed:");
                await message.Channel.SendMessageAsync(
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

            if (!Settings.Singleton.CareToLeakSensitiveExceptions)
            {
                _instrumentationPerfmon.FailedCommandsThatLeakedExceptionInfo.Increment();
                await message.Channel.SendMessageAsync(
                    $"<@!{message.Author.Id}>, [EID-{exceptionID}] An error occured and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                    options: new RequestOptions()
                    {
                        AuditLogReason = "Exception Occurred"
                    }
                );
                await message.Channel.SendMessageAsync(embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build());
                return;
            }

            _instrumentationPerfmon.FailedCommandsThatWerePublicallyMasked.Increment();

            await message.Channel.SendMessageAsync($"<@!{message.Author.Id}>, an unexpected Exception has occurred. Exception ID: {exceptionID}, send this ID to <@!{Settings.Singleton.BotOwnerID}>");
            return;
        }

        public IStateSpecificCommandHandler GetCommandByCommandAlias(string alias)
        {
            lock (_stateSpecificCommandHandlers)
                return (from command in _stateSpecificCommandHandlers where command.CommandAliases.Contains(alias) select command).FirstOrDefault();
        }

        private void ParseAndInsertIntoCommandRegistry()
        {
            lock (_stateSpecificCommandHandlers)
            {
                _instrumentationPerfmon.CommandsParsedAndInsertedIntoRegistry.Increment();
                SystemLogger.Singleton.LifecycleEvent("Begin attempt to register commands via Reflection");

                try
                {
                    var @namespace = GetCommandNamespace();

                    SystemLogger.Singleton.Info("Got command namespace '{0}'.", @namespace);

                    var types = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(@namespace);

                    if (types.Length == 0)
                    {
                        _instrumentationPerfmon.CommandNamespacesThatHadNoClasses.Increment();
                        SystemLogger.Singleton.Warning("There were no commands found in the namespace '{0}'.", @namespace);
                        return;
                    }

                    foreach (var type in types)
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
