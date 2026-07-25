namespace Configuration;

using System;

/// <summary>
/// Implementation for <see cref="BaseProvider"/> that uses Environment variables.
/// </summary>
/// <seealso cref="BaseProvider"/>
/// <seealso cref="IConfigurationProvider"/>
public abstract class EnvironmentProvider : BaseProvider
{
    /// <inheritdoc cref="BaseProvider.SetRawValue{T}(string, T)"/>
    protected override void SetRawValue<T>(string variable, T value)
    {
        var realValue = ConvertFrom(value, typeof(T));

        Environment.SetEnvironmentVariable(variable, realValue);
    }

    /// <inheritdoc cref="BaseProvider.GetRawValue(string, out string)"/>
    protected override bool GetRawValue(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);

        return !string.IsNullOrEmpty(value);
    }
}
