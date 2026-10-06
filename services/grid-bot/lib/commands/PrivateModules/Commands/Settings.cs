namespace Grid.Bot.Commands.Private;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;

using Newtonsoft.Json;

using Microsoft.Extensions.Options;

using Utility;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using IConfigurationRoot = Microsoft.Extensions.Configuration.IConfigurationRoot;

/// <summary>
/// Represents the interaction for settings.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="Settings"/>.
/// </remarks>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <param name="settingsWriter">The <see cref="ISettingsWriter"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="services"/> cannot be null.
/// - <paramref name="settingsWriter"/> cannot be null.
/// </exception>
[LockDownCommand(BotRole.Owner)]
[RequireBotRole(BotRole.Owner)]
[Group("settings"), Summary("Commands used for managing app settings.")]
public class Settings(IServiceProvider services, ISettingsWriter settingsWriter) : ModuleBase
{
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly ISettingsWriter _settingsWriter = settingsWriter ?? throw new ArgumentNullException(nameof(settingsWriter));

    private static readonly Assembly _settingsAssembly = typeof(SettingsSections).Assembly;
    private static readonly Assembly _configAssembly = typeof(IConfiguration).Assembly;

    private const string _notFoundFormat = "The settings section with the name {0} was not found!";

    private static string Format(object value) => SettingsValueFormatter.Format(value);

    // The command-side counterpart of the legacy flat-string formats applied when binding.
    private static bool TryValidate(Type type, string value, out string error)
    {
        error = null;

        var target = Nullable.GetUnderlyingType(type) ?? type;

        try
        {
            if (target == typeof(string)) return true;

            if (target.IsArray)
            {
                var converter = TypeDescriptor.GetConverter(target.GetElementType());

                foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    converter.ConvertFromInvariantString(part);

                return true;
            }

            if (target == typeof(Dictionary<string, string>) || target == typeof(IDictionary<string, string>))
            {
                if (value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(line => line.Split('=').Length == 2))
                    return true;

                error = "Dictionaries must be one key=value pair per line.";

                return false;
            }

            if (string.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(type) != null) return true;

            TypeDescriptor.GetConverter(target).ConvertFromInvariantString(value);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.InnerException?.Message ?? ex.Message;

            return false;
        }
    }

    private object ReadOptions(SettingsSection section, out string error)
    {
        error = null;

        try
        {
            var monitorType = typeof(IOptionsMonitor<>).MakeGenericType(section.OptionsType);
            var monitor = _services.GetService(monitorType);

            return monitorType.GetProperty(nameof(IOptionsMonitor<object>.CurrentValue)).GetValue(monitor);
        }
        catch (Exception ex)
        {
            error = (ex is TargetInvocationException { InnerException: not null } wrapped ? wrapped.InnerException : ex).Message;

            return null;
        }
    }

    private bool TryReload(out string error)
    {
        error = null;

        try
        {
            (_services.GetService(typeof(IConfiguration)) as IConfigurationRoot)?.Reload();

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;

            return false;
        }
    }

    private static PropertyInfo FindProperty(SettingsSection section, string settingName)
        => section.OptionsType.GetProperty(settingName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);

    /// <summary>
    /// Gets information about settings such as environment and versions.
    /// </summary>
    [Command("info"), Summary("Gets information about settings such as environment and versions.")]
    public async Task GetInformationAsync()
    {
        var builder = new EmbedBuilder()
            .WithTitle("Configuration Information")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green);            

        var globalOptions = (_services.GetService(typeof(IOptionsMonitor<GlobalOptions>)) as IOptionsMonitor<GlobalOptions>)?.CurrentValue;
        var isUsingVault = !string.IsNullOrWhiteSpace(globalOptions?.VaultAddress) ? "yes" : "no";
        var settingsAssemblyVersion = _settingsAssembly.GetName().Version;
        var configurationAssemblyVersion = _configAssembly.GetName().Version;

        var environment = Grid.Bot.EnvironmentDataProvider.EnvironmentName;

        builder.AddField("Settings Version", settingsAssemblyVersion, true)
               .AddField("Configuration Version", configurationAssemblyVersion, true)
               .AddField("Environment", environment, true)
               .AddField("Is using Vault", isUsingVault, true);

        await this.ReplyWithReferenceAsync(embed: builder.Build());
    }

    /// <summary>
    /// Gets a list of settings sections that can be modified.
    /// </summary>
    [Command("providers"), Summary("Lists the names of all available settings sections."), Alias("sections")]
    public async Task ListProvidersAsync()
    {
        var builder = new EmbedBuilder()
            .WithTitle("Settings sections")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green)
            .WithDescription($"```\n{string.Join('\n', SettingsSections.All.Select(section => section.Name))}\n```");

        await this.ReplyWithReferenceAsync(embed: builder.Build());
    }

    /// <summary>
    /// Gets the settings for the specified section.
    /// </summary>
    /// <param name="provider">The name of the section.</param>
    /// <param name="refresh">Should the configuration be reloaded beforehand?</param>
    [Command("all"), Summary("Gets all settings for the specified section."), Alias("list")]
    public async Task GetAllAsync(string provider, bool refresh = true)
    {
        using var typing = Context.Channel.EnterTypingState();

        if (!SettingsSections.TryGet(provider, out var section))
        {
            await this.ReplyWithReferenceAsync(string.Format(_notFoundFormat, provider));

            return;
        }

        if (refresh && !TryReload(out var reloadError))
        {
            await this.ReplyWithReferenceAsync($"Failed to reload the configuration: {reloadError}");

            return;
        }

        var options = ReadOptions(section, out var error);
        if (options == null)
        {
            await this.ReplyWithReferenceAsync($"The {section.Name} settings are not valid: {error}");

            return;
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(options, Formatting.Indented)));

        await this.ReplyWithFileAsync(stream, $"{section.Name}.json", "Here are the settings for the specified section.");
    }

    /// <summary>
    /// Get the value of the specified setting.
    /// </summary>
    /// <param name="provider">The name of the section</param>
    /// <param name="settingName">The name of the setting</param>
    /// <param name="refresh">Should the configuration be reloaded beforehand?</param>
    [Command("get"), Summary("Get the value of the specified setting.")]
    public async Task GetSettingAsync(string provider, string settingName, bool refresh = true)
    {
        using var typing = Context.Channel.EnterTypingState();

        if (!SettingsSections.TryGet(provider, out var section))
        {
            await this.ReplyWithReferenceAsync(string.Format(_notFoundFormat, provider));

            return;
        }

        var property = FindProperty(section, settingName);
        if (property == null)
        {
            await this.ReplyWithReferenceAsync($"The settings section with the name {section.Name} does not define the setting {settingName}!");

            return;
        }

        if (refresh && !TryReload(out var reloadError))
        {
            await this.ReplyWithReferenceAsync($"Failed to reload the configuration: {reloadError}");

            return;
        }

        var options = ReadOptions(section, out var error);
        if (options == null)
        {
            await this.ReplyWithReferenceAsync($"The {section.Name} settings are not valid: {error}");

            return;
        }

        var value = property.GetValue(options);
        var text = value is string stringValue ? stringValue : JsonConvert.SerializeObject(value);
        if (string.IsNullOrEmpty(text)) text = "(empty)";

        var embed = new EmbedBuilder()
            .WithTitle($"{section.Name}.{property.Name} ({property.PropertyType.Name})")
            .WithDescription($"```{text}```")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green)
            .Build();

        await this.ReplyWithReferenceAsync(embed: embed);
    }

    /// <summary>
    /// Sets the specified setting to the specified value.
    /// </summary>
    /// <param name="provider">The name of the section</param>
    /// <param name="settingName">The name of the setting</param>
    /// <param name="newValue">The new value of the setting. Lists are comma separated.</param>
    /// <param name="refresh">Should the configuration be reloaded beforehand?</param>
    [Command("set"), Summary("Sets the specified setting to the specified value.")]
    public async Task SetSettingAsync(string provider, string settingName, string newValue = "", bool refresh = true)
    {
        using var typing = Context.Channel.EnterTypingState();

        if (!SettingsSections.TryGet(provider, out var section))
        {
            await this.ReplyWithReferenceAsync(string.Format(_notFoundFormat, provider));

            return;
        }

        var property = FindProperty(section, settingName);
        if (property == null || !property.CanWrite)
        {
            await this.ReplyWithReferenceAsync($"The settings section with the name {section.Name} does not define a writable setting {settingName}!");

            return;
        }

        if (!TryValidate(property.PropertyType, newValue, out var validationError))
        {
            await this.ReplyWithReferenceAsync($"The value is not valid for {property.Name} ({property.PropertyType.Name}): {validationError}");

            return;
        }

        if (refresh && !TryReload(out var reloadError))
        {
            await this.ReplyWithReferenceAsync($"Failed to reload the configuration: {reloadError}");

            return;
        }

        var before = Format(property.GetValue(ReadOptions(section, out _)));

        if (before == newValue)
        {
            await this.ReplyWithReferenceAsync("The value is identical to the current value, not changing!");

            return;
        }

        await _settingsWriter.SetAsync(section.Name, property.Name, newValue);

        var after = Format(property.GetValue(ReadOptions(section, out _)));

        var embed = new EmbedBuilder()
            .WithTitle($"{section.Name}.{property.Name} ({property.PropertyType.Name})")
            .AddField("Before", $"```{(string.IsNullOrEmpty(before) ? "(empty)" : before)}```")
            .AddField("After", $"```{(string.IsNullOrEmpty(after) ? "(empty)" : after)}```")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(after == newValue ? Color.Green : Color.Orange);

        if (after != newValue)
            embed.WithDescription("The effective value differs from what was written. An environment variable or appsettings file likely overrides it.");

        await this.ReplyWithReferenceAsync(embed: embed.Build());
    }

    /// <summary>
    /// Reloads the configuration.
    /// </summary>
    [Command("refresh"), Summary("Reloads the configuration from all sources.")]
    public async Task RefreshAsync()
    {
        using var typing = Context.Channel.EnterTypingState();

        if (!TryReload(out var error))
        {
            await this.ReplyWithReferenceAsync($"Failed to reload the configuration: {error}");

            return;
        }

        await this.ReplyWithReferenceAsync("Reloaded the configuration!");
    }
}
