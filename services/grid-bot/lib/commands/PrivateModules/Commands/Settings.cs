namespace Grid.Bot.Commands.Private;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Discord;
using Discord.Commands;

using Newtonsoft.Json;

using Vault;
using Configuration;

using Utility;
using Extensions;

/// <summary>
/// Represents the interaction for settings.
/// </summary>
[LockDownCommand(BotRole.Owner)]
[RequireBotRole(BotRole.Owner)]
[Group("settings"), Summary("Commands used for managing app settings.")]
public partial class Settings(IServiceProvider services) : ModuleBase
{
    private class ProviderStringConverter : BaseProvider
    {
        public static ProviderStringConverter Singleton = new();

        public object ConvertToPub(string value, Type type) => ConvertTo(value, type);
        public string ConvertFromPub(object value, Type type) => ConvertFrom(value, type);

        protected override bool GetRawValue(string key, out string value)
        {
            throw new NotImplementedException();
        }
        protected override void SetRawValue<T>(string key, T value)
        {
            throw new NotImplementedException();
        }
    }

    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    private const string _namespace = "Grid.Bot";
    private static readonly Assembly _settingsAssembly = Assembly.Load("Shared.Settings");
    private static readonly Assembly _configAssembly = Assembly.Load("Configuration");

    [GeneratedRegex(@"^([a-zA-Z]+)Settings$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private partial Regex GetProviderNameRegex();

    /// <summary>
    /// Gets a list of settings providers that can be modified.
    /// </summary>
    [Command("info"), Summary("Gets information about settings such as environment and versions.")]
    public async Task GetInformationAsync()
    {
        var builder = new EmbedBuilder()
            .WithTitle("Configuration Information")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green);            

        var isUsingVault = VaultClientFactory.Singleton.GetClient() != null ? "yes" : "no";
        var settingsAssemblyVersion = _settingsAssembly.GetName().Version;
        var configurationAssemblyVersion = _configAssembly.GetName().Version;

        var environment = Grid.Bot.EnvironmentProvider.EnvironmentName;

        builder.AddField("Settings Version", settingsAssemblyVersion, true)
               .AddField("Configuration Version", configurationAssemblyVersion, true)
               .AddField("Environment", environment, true)
               .AddField("Is using Vault", isUsingVault, true);

        await this.ReplyWithReferenceAsync(embed: builder.Build());
    }

    /// <summary>
    /// Gets a list of settings providers that can be modified.
    /// </summary>
    [Command("providers"), Summary("Lists the names of all available providers.")]
    public async Task ListProvidersAsync()
    {
        var providerTypes = _settingsAssembly.GetTypes().Where(type => type.BaseType == typeof(BaseSettingsProvider));
        
        var builder = new EmbedBuilder()
            .WithTitle("Providers list")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green);            

        var desc = "```\n";

        foreach (var provider in providerTypes)
            desc += $"{GetProviderNameRegex().Match(provider.Name).Groups[1]}\n";

        desc += "```";

        builder.WithDescription(desc);

        await this.ReplyWithReferenceAsync(embed: builder.Build());
    }

        
    /// <summary>
    /// Gets the settings for the specified provider.
    /// </summary>
    /// <param name="provider">The name of the provider.</param>
    /// <param name="refresh">Should the prvoider be refreshed beforehand?</param>
    [Command("all"), Summary("Gets all settings for the specified provider."), Alias("list")]
    public async Task GetAllAsync(string provider, bool refresh = true)
    {
        using var _ = Context.Channel.EnterTypingState();

        var fullName = $"{provider}settings";

        var type = _settingsAssembly.GetType($"{_namespace}.{fullName}", false, true);
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
        
            return;
        }

        var instance = _services.GetService(type) as BaseSettingsProvider;
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
    
            return;
        }

        if (refresh) instance.Refresh();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(instance.GetRawValues(), Formatting.Indented)));

        await this.ReplyWithFileAsync(stream, $"{provider}.json", "Here are the settings for the specified provider.");
    }

    /// <summary>
    /// Get the value of the specified setting.
    /// </summary>
    /// <param name="provider">The name of the provider</param>
    /// <param name="settingName">The name of the setting</param>
    /// <param name="refresh">Should the prvoider be refreshed beforehand?</param>
    [Command("get"), Summary("Get the value of the specified setting.")]
    public async Task GetSettingAsync(string provider, string settingName, bool refresh = true)
    {
        using var _ = Context.Channel.EnterTypingState();

        var fullName = $"{provider}settings";

        var type = _settingsAssembly.GetType($"{_namespace}.{fullName}", false, true);
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
        
            return;
        }

        var instance = _services.GetService(type) as BaseSettingsProvider;
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
    
            return;
        }

        var property = type.GetProperty(settingName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (property == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} does not define the setting {settingName}!");
    
            return;
        }

        if (refresh) instance.Refresh();

        var value = property.GetMethod.Invoke(instance, []);
        if (value is not string) value = JsonConvert.SerializeObject(value);
        if (string.IsNullOrEmpty(value as string)) value = "(empty)";

        var embed = new EmbedBuilder()
            .WithTitle($"{type.Name}.{property.Name} ({property.PropertyType.Name})")
            .WithDescription($"```{value}```")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green)
            .Build();

        await this.ReplyWithReferenceAsync(embed: embed);
    }

    /// <summary>
    /// Sets the specified setting to the specified value.
    /// </summary>
    /// <param name="provider">The name of the provider</param>
    /// <param name="settingName">The name of the setting</param>
    /// <param name="newValue">The new value of the setting</param>
    /// <param name="refresh">Should the prvoider be refreshed beforehand?</param>
    [Command("set"), Summary("Sets the specified setting to the specified value.")]
    public async Task SetSettingAsync(string provider, string settingName, string newValue = "", bool refresh = true)
    {
        using var _ = Context.Channel.EnterTypingState();

        var fullName = $"{provider}settings";

        var type = _settingsAssembly.GetType($"{_namespace}.{fullName}", false, true);
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
        
            return;
        }

        var instance = _services.GetService(type) as BaseSettingsProvider;
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
    
            return;
        }

        var property = type.GetProperty(settingName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (property == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} does not define the setting {settingName}!");
    
            return;
        }

        if (refresh) instance.Refresh();

        var value = property.GetMethod.Invoke(instance, []);
        var converted = ProviderStringConverter.Singleton.ConvertToPub(newValue, property.PropertyType);

        if (value.Equals(converted))
        {
            await this.ReplyWithReferenceAsync("The value is identical to the current value, not changing!");

            return;
        }

        var attribute = property.GetCustomAttribute<SettingNameAttribute>();
        var name = attribute?.Name ?? property.Name;

        var genericSet = type.GetMethod("Set", BindingFlags.Instance | BindingFlags.Public).MakeGenericMethod([ property.PropertyType ]);

        genericSet.Invoke(instance, [name, converted]);

        if (value is not string) value = JsonConvert.SerializeObject(value);
        if (string.IsNullOrEmpty(value as string)) value = "(empty)";

        var embed = new EmbedBuilder()
            .WithTitle($"{type.Name}.{property.Name} ({property.PropertyType.Name})")
            .AddField("Before", $"```{value}```")
            .AddField("After", $"```{newValue}```")
            .WithAuthor(Context.User)
            .WithCurrentTimestamp()
            .WithColor(Color.Green)
            .Build();

        await this.ReplyWithReferenceAsync(embed: embed);
    }
    
    /// <summary>
    /// Refreshes the specified provider or all registered providers.
    /// </summary>
    /// <param name="provider">The name of the provider.</param>
    [Command("refresh"), Summary("Refreshes the specified provider or all registered providers.")]
    public async Task RefreshAsync(string provider = "")
    {
        using var _ = Context.Channel.EnterTypingState();

        if (string.IsNullOrEmpty(provider))
        {
            VaultProvider.RefreshAllProviders();

            await this.ReplyWithReferenceAsync("Refreshed all registered providers!");

            return;
        }

        var fullName = $"{provider}settings";

        var type = _settingsAssembly.GetType($"{_namespace}.{fullName}", false, true);
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
        
            return;
        }

        var instance = _services.GetService(type) as BaseSettingsProvider;
        if (type == null)
        {
            await this.ReplyWithReferenceAsync($"The settings provider with the name {provider} was not found!");
    
            return;
        }

        instance.Refresh();

        await this.ReplyWithReferenceAsync($"Successfully refreshed the {provider} settings provider!");
    }
}
