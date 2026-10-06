namespace Grid.Bot;

using System.Collections.Generic;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="GrpcOptions"/>.
/// </summary>
public class GrpcOptionsValidator : IValidateOptions<GrpcOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string name, GrpcOptions options)
    {
        if (!options.GridBotGrpcServerEnabled || !options.GrpcServerUseTls)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.GrpcServerCertificatePath))
            failures.Add($"'{nameof(options.GrpcServerCertificatePath)}' is required when '{nameof(options.GrpcServerUseTls)}' is true.");

        if (string.IsNullOrWhiteSpace(options.GrpcServerCertificatePassword))
            failures.Add($"'{nameof(options.GrpcServerCertificatePassword)}' is required when '{nameof(options.GrpcServerUseTls)}' is true.");

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
