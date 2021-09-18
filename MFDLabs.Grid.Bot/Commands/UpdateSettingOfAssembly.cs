using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Reflection;
using MFDLabs.Text.Extensions;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class UpdateSettingOfAssembly : IStateSpecificCommandHandler
    {
        public string CommandName => "Update Setting Of Assembly ";

        public string CommandDescription => "Updates a setting inside of a different assembly";

        public string[] CommandAliases => new string[] { "upa", "updateofassembly" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var assemblyName = messageContentArray.ElementAtOrDefault(0);

            if (assemblyName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null assembly name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, expected the \"AssemblyName\" to be not null or not empty.");
                return;
            }

            var settingsGroupName = messageContentArray.ElementAtOrDefault(1);

            if (settingsGroupName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null setting group name, aborting.");
                await message.ReplyAsync("The second parameter of the command was null, expected the \"SettingsGroupName\" to be not null or not empty.");
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
                await message.ReplyAsync($"Could not find the assembly by the name of '{assemblyName}'. Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }
            catch (TypeLoadException)
            {
                SystemLogger.Singleton.Warning("Could not find the type '{0}' in the assembly '{1}', aborting.", settingsGroupName, assemblyName);
                await message.ReplyAsync($"Could not find the type by the name of '{settingsGroupName}' in the assembly '{assemblyName}'. Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            if (remoteSettings.BaseType != typeof(ApplicationSettingsBase))
            {
                SystemLogger.Singleton.Warning("The type '{0}' in the assembly '{1}' did not extend the type '{2}', aborting.", remoteSettings.FullName, remoteSettings.Assembly.FullName, typeof(ApplicationSettingsBase).FullName);
                await message.ReplyAsync($"The type '{remoteSettings.FullName}' in the assembly '{remoteSettings.Assembly.GetName().Name}' did not extend the type '{typeof(ApplicationSettingsBase).FullName}'. Please check to make sure you spelled it correctly! (CaSe-SeNsItIvE)");
                return;
            }

            var settingsInstance = remoteSettings.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);

            if (settingsInstance == null)
            {
                SystemLogger.Singleton.Warning("The property 'Default' on the type '{0}' in the assembly '{1}' was null, aborting.", remoteSettings.FullName, remoteSettings.Assembly.FullName);
                await message.ReplyAsync($"The property 'Default' on the type '{remoteSettings.FullName}' in the assembly '{remoteSettings.Assembly.GetName().Name}' was null. This is an issue with an unitialized settings group, please try again later.");
                return;
            }

            var settingInstanceValue = Convert.ChangeType(settingsInstance.GetValue(null), remoteSettings);

            var settingName = messageContentArray.ElementAtOrDefault(2);

            if (settingName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null Setting name, aborting.");
                await message.ReplyAsync("The third parameter of the command was null, expected the \"SettingName\" to be not null or not empty.");
                return;
            }

            // TODO: Case sensitivity on production??

            var rawSettingValue = string.Join(" ", messageContentArray.Skip(3).Take(messageContentArray.Length - 1));

            if (rawSettingValue.IsNullOrEmpty())
            {
                if (!Settings.Singleton.AllowNullsWhenUpdatingSetting)
                {
                    SystemLogger.Singleton.Warning("The environment does not allow nulls.");
                    await message.ReplyAsync("The setting 'AllowNullsWhenUpdatingSetting' is disabled, please supply a 'non-nullable' value.");
                    return;
                }

                rawSettingValue = "";
            }

            var indexer = settingInstanceValue.GetType().GetProperties().FirstOrDefault(a =>
            {
                var p = a.GetIndexParameters();

                return p.Length == 1
                    && p.FirstOrDefault(b => b.ParameterType == typeof(string)) != null;
            });

            if (indexer == null)
            {
                SystemLogger.Singleton.Warning("The indexer for the property '{0}' on the type '{1}' in the assembly '{2}' was null, aborting.", settingName, remoteSettings.FullName, remoteSettings.Assembly.FullName);
                return;
            }


            object setting;

            try
            {
                setting = indexer.GetValue(settingInstanceValue, new[] { settingName });
            }
            catch (TargetInvocationException tEx)
            {
                if (tEx.InnerException is SettingsPropertyNotFoundException ex)
                {
                    SystemLogger.Singleton.Warning(ex.Message);
                    await message.ReplyAsync($"Could not find the setting '{settingName}' in the setting group '{remoteSettings.FullName}' in the assembly '{remoteSettings.Assembly.GetName().Name}'");
                    return;
                }

                SystemLogger.Singleton.Warning(tEx.Message);
                await message.ReplyAsync("Unknown exception occurred when updating setting.");
                return;
            }

            var type = setting.GetType();
            object transformedValue;

            try
            {
                if (type.IsEnum)
                {
                    transformedValue = Enum.Parse(type, rawSettingValue);
                }
                else if (TypeHelper.IsPrimitive(type))
                {
                    transformedValue = Convert.ChangeType(rawSettingValue, type);
                }
                else if (type == typeof(TimeSpan))
                {
                    // Specific here because the only class we actually use in settings is TimeSpan, everything else is either a primitive or Enum
                    transformedValue = TimeSpan.Parse(rawSettingValue);
                }
                else
                {
                    transformedValue = rawSettingValue;
                }
            }
            catch (Exception ex)
            {
                SystemLogger.Singleton.Warning(ex.Message);

                if (ex is ArgumentNullException || ex is ArgumentException)
                {
                    await message.ReplyAsync($"There was an argument exception with your setting value when trying to cast it to '{type.FullName}', {ex.Message}.");
                    return;
                }

                if (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
                {
                    await message.ReplyAsync($"The typeof your setting value could not be casted to the type of the real setting value, which is  '{type.FullName}', please try again.");
                    return;
                }

                await message.ReplyAsync($"An unknown exception occurred when trying to update the setting '{settingName}'.");

                return;
            }

            try
            {
                indexer.SetValue(settingInstanceValue, transformedValue, new[] { settingName });
            }
            catch (SettingsPropertyIsReadOnlyException ex)
            {
                SystemLogger.Singleton.Warning(ex.Message);
                await message.ReplyAsync(ex.Message);
                return;
            }

            var saveInvoker = settingInstanceValue.GetType().GetMethod("Save", BindingFlags.Public | BindingFlags.Instance);

            if (saveInvoker == null)
            {
                SystemLogger.Singleton.Warning("The 'Save' method on the type '{0}' in the assembly '{1}' was not present, aborting.", remoteSettings.FullName, remoteSettings.Assembly.FullName);
                await message.ReplyAsync($"The 'Save' method on the type '{remoteSettings.FullName}' in the assembly '{remoteSettings.Assembly.GetName().Name}' was not present. This is an issue with a broken settings group, please try again later.");
                return;
            }

            saveInvoker.Invoke(settingInstanceValue, null);
            SystemLogger.Singleton.LifecycleEvent("Successfully set the setting '{0}' to the value of '{1}' in the settings group '{2}' in the assembly '{3}'.", settingName, (bool)(transformedValue?.ToString().IsNullOrEmpty()) ? "null" : transformedValue, remoteSettings.FullName, remoteSettings.Assembly.GetName().Name);
            await message.ReplyAsync($"Successfully set the setting '{settingName}' to the value of '{((bool)(transformedValue?.ToString().IsNullOrEmpty()) ? "null" : transformedValue)}' in the settings group '{remoteSettings.FullName}' in the assembly '{remoteSettings.Assembly.GetName().Name}'.");
        }
    }
}
