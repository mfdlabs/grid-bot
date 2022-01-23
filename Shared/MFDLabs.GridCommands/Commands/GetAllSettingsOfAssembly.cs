﻿/*

WARNING:

THIS IS NOT ALLOWED BECAUSE OF THE WAY INDEXERS WORK, PLEASE TRY AGAIN LATER!!!!

!!!! I could try filter out all of the properties that are used by the ApplicationSettingsBase...? !!!!
 
*/

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GetAllSettingsOfAssembly : IStateSpecificCommandHandler
    {
        public string CommandName => "Get All Remote Settings";
        public string CommandDescription => "Attempts to list all remote settings for a different assembly, " +
                                            "if the command was invoked in a public channel (a channel that is an " +
                                            "instance of SocketGuildChannel) and the setting 'AllowLogSettingsInPublicChannels' " +
                                            "is disabled, it will throw.";
        public string[] CommandAliases => new[] { "getalla", "getallassemblysettings" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc/>
        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            if (message.IsInPublicChannel() && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowLogSettingsInPublicChannels)
            {
                await message.ReplyAsync("Are you sure you want to do that? This will log sensitive things!");
                return;
            }

            var assemblyName = messageContentArray.ElementAtOrDefault(0);

            if (assemblyName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null assembly name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, expected the " +
                                         "\"AssemblyName\" to be not null or not empty.");
                return;
            }

            var settingsGroupName = messageContentArray.ElementAtOrDefault(1);

            if (settingsGroupName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null setting group name, aborting.");
                await message.ReplyAsync("The second parameter of the command was null, expected the " +
                                         "\"SettingsGroupName\" to be not null or not empty.");
                return;
            }

            Type remoteSettings;

            try
            {
                remoteSettings = Type.GetType($"{settingsGroupName}, {assemblyName}", true);
            }
            catch (FileNotFoundException)
            {
                SystemLogger.Singleton.Warning("Could not find the assembly '{0}', aborting.", assemblyName);
                await message.ReplyAsync($"Could not find the assembly by the name of '{assemblyName}'." +
                                         $"Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }
            catch (TypeLoadException)
            {
                SystemLogger.Singleton.Warning("Could not find the type '{0}' in the assembly '{1}', aborting.",
                    settingsGroupName,
                    assemblyName);
                await message.ReplyAsync($"Could not find the type by the name of '{settingsGroupName}' in " +
                                         $"the assembly '{assemblyName}'. Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            if (remoteSettings.BaseType != typeof(ApplicationSettingsBase))
            {
                SystemLogger.Singleton.Warning(
                    "The type '{0}' in the assembly '{1}' did not extend the type '{2}', aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName,
                    typeof(ApplicationSettingsBase).FullName);
                await message.ReplyAsync($"The type '{remoteSettings.FullName}' in the assembly" +
                                         $"'{remoteSettings.Assembly.GetName().Name}' did not extend the type" +
                                         $"'{typeof(ApplicationSettingsBase).FullName}'. Please check to make" +
                                         $"sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            var settingsInstance = remoteSettings.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);

            if (settingsInstance == null)
            {
                SystemLogger.Singleton.Warning("The property 'Default' on the type '{0}' in the assembly '{1}'" +
                                               "was null, aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName);
                await message.ReplyAsync($"The property 'Default' on the type '{remoteSettings.FullName}' in" +
                                         $"the assembly '{remoteSettings.Assembly.GetName().Name}' was null." +
                                         $"This is an issue with an unitialized settings group, please try again later.");
                return;
            }

            var settingInstanceValue = Convert.ChangeType(settingsInstance.GetValue(null), remoteSettings);


            var indexer = settingInstanceValue.GetType().GetProperties().FirstOrDefault(a =>
            {
                var p = a.GetIndexParameters();

                return p.Length == 1
                    && p.FirstOrDefault(b => b.ParameterType == typeof(string)) != null;
            });

            throw new ApplicationException("Partially disabled due to not being able to get all members of an index property :(.");
        }
    }
}