namespace Grid.Bot;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="TelemetryOptions"/>.
/// </summary>
public class TelemetryOptionsValidator : IValidateOptions<TelemetryOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string name, TelemetryOptions options)
    {
        var failures = new List<string>();

        if (options.TracingEnabled)
        {
            if (!Uri.TryCreate(options.TracingOtlpEndpoint, UriKind.Absolute, out _))
                failures.Add($"'{nameof(options.TracingOtlpEndpoint)}' must be an absolute URI when '{nameof(options.TracingEnabled)}' is true.");

            if (options.TracingSampleRatio is < 0 or > 1 || double.IsNaN(options.TracingSampleRatio))
                failures.Add($"'{nameof(options.TracingSampleRatio)}' must be between 0 and 1.");

            if (string.IsNullOrWhiteSpace(options.TracingServiceName))
                failures.Add($"'{nameof(options.TracingServiceName)}' is required when '{nameof(options.TracingEnabled)}' is true.");
        }

        if (options.LoggingEnabled)
        {
            if (!Uri.TryCreate(options.LoggingOtlpEndpoint, UriKind.Absolute, out _))
                failures.Add($"'{nameof(options.LoggingOtlpEndpoint)}' must be an absolute URI when '{nameof(options.LoggingEnabled)}' is true.");

            // The exporter uses an explicit HTTP endpoint verbatim, so a bare host posts to "/" and is rejected.
            if (options.LoggingOtlpProtocol == TracingOtlpProtocol.HttpProtobuf
                && Uri.TryCreate(options.LoggingOtlpEndpoint, UriKind.Absolute, out var loggingUri)
                && loggingUri.AbsolutePath.Trim('/').Length == 0)
                failures.Add($"'{nameof(options.LoggingOtlpEndpoint)}' must include the signal path (for Loki, '/otlp/v1/logs') when using HttpProtobuf.");

            if (string.IsNullOrWhiteSpace(options.LoggingServiceName))
                failures.Add($"'{nameof(options.LoggingServiceName)}' is required when '{nameof(options.LoggingEnabled)}' is true.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
