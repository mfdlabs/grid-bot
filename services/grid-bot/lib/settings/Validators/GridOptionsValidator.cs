namespace Grid.Bot;

using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="GridOptions"/>.
/// </summary>
public class GridOptionsValidator : IValidateOptions<GridOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string name, GridOptions options)
    {
#if DEBUG
        if (options.DebugUseNoopJobManager)
            return ValidateOptionsResult.Success;
#endif

        var failures = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Require(failures, nameof(options.GridServerRegistryKeyName), options.GridServerRegistryKeyName);
            Require(failures, nameof(options.GridServerRegistryValueName), options.GridServerRegistryValueName);
        }
        else
        {
            Require(failures, nameof(options.GridServerImageName), options.GridServerImageName);
            Require(failures, nameof(options.GridServerImageTag), options.GridServerImageTag);
            Require(failures, nameof(options.GridServerSettingsKey), options.GridServerSettingsKey);
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }

    private static void Require(List<string> failures, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            failures.Add($"'{name}' is required.");
    }
}
