namespace Grid.Bot;

using System.Collections.Generic;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="RecoveryOptions"/>.
/// </summary>
public class RecoveryOptionsValidator : IValidateOptions<RecoveryOptions>
{
    /// <inheritdoc cref="IValidateOptions{T}.Validate(string?, T)"/>
    public ValidateOptionsResult Validate(string name, RecoveryOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BotToken))
            failures.Add($"{nameof(options.BotToken)} is required.");

        if (!options.StandaloneMode)
        {
            if (string.IsNullOrWhiteSpace(options.GridBotEndpoint))
                failures.Add($"{nameof(options.GridBotEndpoint)} is required when not in standalone mode.");

            if (string.IsNullOrWhiteSpace(options.DiscordWebhookUrl))
                failures.Add($"{nameof(options.DiscordWebhookUrl)} is required when not in standalone mode.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
