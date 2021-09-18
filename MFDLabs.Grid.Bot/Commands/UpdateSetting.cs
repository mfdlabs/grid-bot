using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Text.Extensions;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class UpdateSetting : IStateSpecificCommandHandler
    {
        public string CommandName => "Update Setting";

        public string CommandDescription => "Updates a setting";

        public string[] CommandAliases => new string[] { "up", "update" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var settingName = messageContentArray.ElementAtOrDefault(0);

            if (settingName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null Setting name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, expected the \"SettingName\" to be not null or not empty.");
                return;
            }

            var rawSettingValue = string.Join(" ", messageContentArray.Skip(1).Take(messageContentArray.Length - 1));

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

            object setting;

            try
            {
                setting = global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName];
            }
            catch (SettingsPropertyNotFoundException ex)
            {
                SystemLogger.Singleton.Warning(ex.Message);
                await message.ReplyAsync(ex.Message);
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
                else if (type.IsPrimitave())
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
                global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName] = transformedValue;
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
                SystemLogger.Singleton.LifecycleEvent("Successfully set the setting '{0}' to the value of '{1}'.", settingName, (bool)(transformedValue?.ToString().IsNullOrEmpty()) ? "null" : transformedValue);
                await message.ReplyAsync($"Successfully set the setting '{settingName}' to the value of '{((bool)(transformedValue?.ToString().IsNullOrEmpty()) ? "null" : transformedValue)}'.");
            }
            catch (SettingsPropertyIsReadOnlyException ex)
            {
                SystemLogger.Singleton.Warning(ex.Message);
                await message.ReplyAsync(ex.Message);
                return;
            }
        }
    }
}
