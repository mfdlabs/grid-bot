using System;
using System.Linq;
using System.Configuration;
using System.Threading.Tasks;

using Discord.WebSocket;

using Logging;

using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Reflection.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class UpdateSetting : IStateSpecificCommandHandler
    {
        public string CommandName => "Update Bot Instance Setting";
        public string CommandDescription => "Attempts to update the value of a setting from " +
                                            $"'{typeof(global::MFDLabs.Grid.Bot.Properties.Settings).FullName}', " +
                                            "if the setting is not found it throws, if the setting value cannot " +
                                            "be converted it will throw.\nLayout: " +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}update settingName ...settingValue";
        public string[] CommandAliases => new[] { "up", "update" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var settingName = messageContentArray.ElementAtOrDefault(0);

            if (settingName.IsNullOrEmpty())
            {
                Logger.Singleton.Warning("Null Setting name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, expected " +
                                         "the \"SettingName\" to be not null or not empty.");
                return;
            }

            var rawSettingValue = string.Join(" ", messageContentArray.Skip(1).Take(messageContentArray.Length - 1));

            if (rawSettingValue.IsNullOrEmpty())
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowNullsWhenUpdatingSetting)
                {
                    Logger.Singleton.Warning("The environment does not allow nulls.");
                    await message.ReplyAsync("The setting 'AllowNullsWhenUpdatingSetting' is disabled, " +
                                             "please supply a 'non-nullable' value.");
                    return;
                }

                rawSettingValue = "";
            }

            object setting;

            try
            {
                setting = global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName!];
            }
            catch (SettingsPropertyNotFoundException ex)
            {
                Logger.Singleton.Warning(ex.Message);
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
                else if (type.IsPrimitive())
                {
                    transformedValue = Convert.ChangeType(rawSettingValue, type);
                }
                else if (type == typeof(TimeSpan))
                {
                    // Specific here because the only class we actually use in settings is
                    // TimeSpan, everything else is either a primitive or Enum
                    transformedValue = TimeSpan.Parse(rawSettingValue);
                }
                else
                {
                    transformedValue = rawSettingValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Singleton.Warning(ex.Message);

                switch (ex)
                {
                    case ArgumentNullException or ArgumentException:
                        await message.ReplyAsync("There was an argument exception with your setting value " +
                                                 $"when trying to cast it to '{type.FullName}', {ex.Message}.");
                        return;
                    case InvalidCastException:
                    case FormatException:
                    case OverflowException:
                        await message.ReplyAsync("The typeof your setting value could not be casted to the " +
                                                 $"type of the real setting value, which is  '{type.FullName}', please try again.");
                        return;
                    default:
                        await message.ReplyAsync($"An unknown exception occurred when trying to update the setting '{settingName}'.");

                        return;
                }
            }

            try
            {
                global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName] = transformedValue;
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
                Logger.Singleton.Debug("Successfully set the setting " +
                                                      "'{0}' to the value of '{1}'.",
                    settingName,
                    // ReSharper disable once PossibleInvalidOperationException
                    (bool) transformedValue?.ToString()
                        .IsNullOrEmpty()
                        ? "null"
                        : transformedValue);
                await message.ReplyAsync($"Successfully set the setting '{settingName}' to the" +
                                         $" value of '{(transformedValue.ToString().IsNullOrEmpty() ? "null" : transformedValue)}'.");
            }
            catch (SettingsPropertyIsReadOnlyException ex)
            {
                Logger.Singleton.Warning(ex.Message);
                await message.ReplyAsync(ex.Message);
            }
        }
    }
}
