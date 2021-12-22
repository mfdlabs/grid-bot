using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GetSettingOfAssembly : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Remote Setting";
        public string CommandDescription => $"Attempts to get an item from a remote settings instance for a different " +
                                            $"assembly, if the assembly is not found it will throw, if the settings " +
                                            $"instance is not found it will throw, if the settings are not A" +
                                            $"pplicationSettingsBase, it will throw.\nLayout: " +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}getassemblysetting " +
                                            $"assemblyName settingsInstanceName settingName.";
        public string[] CommandAliases => new[] { "geta", "getassemblysetting" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var assemblyName = messageContentArray.ElementAtOrDefault(0);

            if (assemblyName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null assembly name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, " +
                                         "expected the \"AssemblyName\" to be not null or not empty.");
                return;
            }

            var settingsGroupName = messageContentArray.ElementAtOrDefault(1);

            if (settingsGroupName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null setting group name, aborting.");
                await message.ReplyAsync("The second parameter of the command was null, " +
                                         "expected the \"SettingsGroupName\" to be not null or not empty.");
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
                                         $" Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }
            catch (TypeLoadException)
            {
                SystemLogger.Singleton.Warning("Could not find the type '{0}' in the assembly '{1}', aborting.", settingsGroupName, assemblyName);
                await message.ReplyAsync($"Could not find the type by the name of '{settingsGroupName}' " +
                                         $"in the assembly '{assemblyName}'. Please check to make sure you spelled " +
                                         $"it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            if (remoteSettings.BaseType != typeof(ApplicationSettingsBase))
            {
                SystemLogger.Singleton.Warning("The type '{0}' in the assembly '{1}' did not extend the type '{2}'," +
                                               " aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName,
                    typeof(ApplicationSettingsBase).FullName);
                await message.ReplyAsync($"The type '{remoteSettings.FullName}' in the assembly " +
                                         $"'{remoteSettings.Assembly.GetName().Name}' did not extend the type " +
                                         $"'{typeof(ApplicationSettingsBase).FullName}'. Please check to make sure you " +
                                         $"spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            var settingsInstance = remoteSettings.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);

            if (settingsInstance == null)
            {
                SystemLogger.Singleton.Warning("The property 'Default' on the type '{0}' in the assembly '{1}' " +
                                               "was null, aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName);
                await message.ReplyAsync($"The property 'Default' on the type '{remoteSettings.FullName}' in " +
                                         $"the assembly '{remoteSettings.Assembly.GetName().Name}' was null. " +
                                         "This is an issue with an unitialized settings group, please try again later.");
                return;
            }

            var settingInstanceValue = Convert.ChangeType(settingsInstance.GetValue(null), remoteSettings);

            var settingName = messageContentArray.ElementAtOrDefault(2);

            if (settingName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null Setting name, aborting.");
                await message.ReplyAsync("The third parameter of the command was null, expected the \"SettingName\"" +
                                         "to be not null or not empty.");
                return;
            }

            var indexer = settingInstanceValue.GetType().GetProperties().FirstOrDefault(a =>
            {
                var p = a.GetIndexParameters();

                return p.Length == 1
                    && p.FirstOrDefault(b => b.ParameterType == typeof(string)) != null;
            });

            if (indexer == null)
            {
                SystemLogger.Singleton.Warning("The indexer for the property '{0}' on the type '{1}' in the" +
                                               "assembly '{2}' was null, aborting.",
                    settingName,
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName);
                return;
            }


            object setting;

            try
            {
                setting = indexer.GetValue(settingInstanceValue, new object[] { settingName });
            }
            catch (TargetInvocationException tEx)
            {
                if (tEx.InnerException is SettingsPropertyNotFoundException ex)
                {
                    SystemLogger.Singleton.Warning(ex.Message);
                    await message.ReplyAsync($"Could not find the setting '{settingName}' in the setting " +
                                             $"group '{remoteSettings.FullName}' in the" +
                                             $" assembly '{remoteSettings.Assembly.GetName().Name}'");
                    return;
                }

                SystemLogger.Singleton.Warning(tEx.Message);
                await message.ReplyAsync("Unknown exception occurred when getting setting.");
                return;
            }

            await message.Channel.SendMessageAsync(
                embed: new EmbedBuilder()
                        .WithTitle($"'{settingName}' of '{remoteSettings.FullName}' in " +
                                   $"'{remoteSettings.Assembly.GetName().Name}'")
                        .WithDescription($"```\n{setting}\n```")
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
            );
        }
    }
}
