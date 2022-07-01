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
    internal sealed class ReloadSettingsOfAssembly : IStateSpecificCommandHandler
    {
        public string CommandName => "Reload Remote Settings";
        public string CommandDescription => "Attempts to reload remote settings from a different assembly," +
                                            "if the assembly is not found it throws, if the " +
                                            "settings instance is not found it throws, if the " +
                                            "settings instance is not a ApplicationSettingsBase child, it throws.";
        public string[] CommandAliases => new[] { "reloada", "reloadassemblysettings" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var assemblyName = messageContentArray.ElementAtOrDefault(0);

            if (assemblyName.IsNullOrEmpty())
            {
                Logger.Singleton.Warning("Null assembly name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, " +
                                         "expected the \"AssemblyName\" to be not null or not empty.");
                return;
            }

            var settingsGroupName = messageContentArray.ElementAtOrDefault(1);

            if (settingsGroupName.IsNullOrEmpty())
            {
                Logger.Singleton.Warning("Null setting group name, aborting.");
                await message.ReplyAsync("The second parameter of the command was null," +
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
                Logger.Singleton.Warning("Could not find the assembly '{0}', aborting.", assemblyName);
                await message.ReplyAsync($"Could not find the assembly by the name of" +
                                         $"'{assemblyName}'. Please check to make sure you spelled " +
                                         $"it correctly! (CaSe-SeNsItIvE)");
                return;
            }
            catch (TypeLoadException)
            {
                Logger.Singleton.Warning("Could not find the type '{0}' in the assembly '{1}', aborting.", settingsGroupName, assemblyName);
                await message.ReplyAsync($"Could not find the type by the name of" +
                                         $"'{settingsGroupName}' in the assembly '{assemblyName}'. " +
                                         $"Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            if (remoteSettings.BaseType != typeof(ApplicationSettingsBase))
            {
                Logger.Singleton.Warning(
                    "The type '{0}' in the assembly '{1}' did not extend the type '{2}', aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName,
                    typeof(ApplicationSettingsBase).FullName);
                await message.ReplyAsync($"The type '{remoteSettings.FullName}' in the" +
                                         $"assembly '{remoteSettings.Assembly.GetName().Name}'" +
                                         $"did not extend the type '{typeof(ApplicationSettingsBase).FullName}'." +
                                         $"Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            var settingsInstance = remoteSettings.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);

            if (settingsInstance == null)
            {
                Logger.Singleton.Warning(
                    "The property 'Default' on the type '{0}' in the assembly '{1}' was null, aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName);
                await message.ReplyAsync($"The property 'Default' on the type '{remoteSettings.FullName}'" +
                                         $" in the assembly '{remoteSettings.Assembly.GetName().Name}' was null. " +
                                         $"This is an issue with an unitialized settings group, please try again later.");
                return;
            }

            var settingInstanceValue = Convert.ChangeType(settingsInstance.GetValue(null), remoteSettings);


            var reload = settingInstanceValue.GetType().GetMethod("Reload", BindingFlags.Public | BindingFlags.Instance);

            if (reload == null)
            {
                Logger.Singleton.Warning(
                    "The 'Reload' method on the type '{0}' in the assembly '{1}' was not present, aborting.",
                    remoteSettings.FullName,
                    remoteSettings.Assembly.FullName);
                await message.ReplyAsync($"The 'Reload' method on the type '{remoteSettings.FullName}' " +
                                         $"in the assembly '{remoteSettings.Assembly.GetName().Name}' was not present. " +
                                         $"This is an issue with a broken settings group, please try again later.");
                return;
            }

            reload.Invoke(settingInstanceValue, null);

            await message.ReplyAsync($"Successfully reloaded all settings for the group '{remoteSettings.FullName}' " +
                                     $"in the assembly '{remoteSettings.Assembly.GetName().Name}'" +
                                     $"from {remoteSettings.Assembly.GetName().Name}.dll.config");
        }
    }
}
