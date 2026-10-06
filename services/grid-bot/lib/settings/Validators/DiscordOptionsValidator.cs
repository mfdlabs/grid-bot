namespace Grid.Bot;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="DiscordOptions"/>.
/// </summary>
public class DiscordOptionsValidator : IValidateOptions<DiscordOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string name, DiscordOptions options)
    {
#if !DEBUG
        if (string.IsNullOrWhiteSpace(options.BotToken))
            return ValidateOptionsResult.Fail($"'{nameof(options.BotToken)}' is required.");
#endif

        return ValidateOptionsResult.Success;
    }
}
