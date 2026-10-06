namespace Grid.Bot;

using System.Collections.Generic;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="WebOptions"/>.
/// </summary>
public class WebOptionsValidator : IValidateOptions<WebOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string name, WebOptions options)
    {
        if (!options.IsWebServerEnabled || !options.WebServerUseTls)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.WebServerCertificatePath))
            failures.Add($"'{nameof(options.WebServerCertificatePath)}' is required when '{nameof(options.WebServerUseTls)}' is true.");

        if (string.IsNullOrWhiteSpace(options.WebServerCertificatePassword))
            failures.Add($"'{nameof(options.WebServerCertificatePassword)}' is required when '{nameof(options.WebServerUseTls)}' is true.");

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
