namespace Grid.Bot;

using System;

using Logging;

/// <summary>
/// Options for global entrypoint settings.
/// </summary>
public class GlobalOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Global";

    /// <summary>
    /// The vault path for the global options.
    /// </summary>
    public const string VaultPath = "global";

    /// <summary>
    /// Gets or sets the name of the default logger.
    /// </summary>
    public string DefaultLoggerName { get; set; } = "bot";

    /// <summary>
    /// Gets or sets the log level for the default logger.
    /// </summary>
    public LogLevel DefaultLoggerLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the address for the metrics server.
    /// </summary>
    public string MetricsBindAddress { get; set; } = "http://+:8081/";

    /// <summary>
    /// Gets or sets a value indicating whether the default logger logs to console.
    /// </summary>
    public bool DefaultLoggerLogToConsole { get; set; } = true;

    /// <summary>
    /// Gets or sets the Discord URL for the primary support guild.
    /// </summary>
    public string SupportGuildDiscordUrl { get; set; } = "https://discord.gg/hdg2z6bm5c";

    /// <summary>
    /// Gets or sets the GitHub url for the support hub.
    /// </summary>
    public string SupportHubGitHubUrl { get; set; } = "https://github.com/mfdlabs/grid-bot-support";

    /// <summary>
    /// Gets or sets the Url for the documentation hub.
    /// </summary>
    public string DocumentationHubUrl { get; set; } = "https://grid-bot.ops.vmminfra.net";

    /// <summary>
    /// Gets or sets the Vault address, falling back to VAULT_ADDR.
    /// </summary>
    public string VaultAddress { get; set; } = Environment.GetEnvironmentVariable("VAULT_ADDR");

    /// <summary>
    /// Gets or sets the Vault token or AppRole credential, falling back to VAULT_CREDENTIAL then VAULT_TOKEN.
    /// </summary>
    public string VaultCredential { get; set; } =
        Environment.GetEnvironmentVariable("VAULT_CREDENTIAL") ?? Environment.GetEnvironmentVariable("VAULT_TOKEN");

    /// <summary>
    /// Gets or sets a value indicating whether alerting via Discord webhook is enabled.
    /// </summary>
    public bool DiscordWebhookAlertingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Discord webhook URL for alerts.
    /// </summary>
    public string DiscordWebhookUrl { get; set; } = string.Empty;
}
