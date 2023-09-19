namespace Configuration;

using System;

/// <summary>
/// Implementation for <see cref="BaseProvider"/> that uses Environment variables.
/// </summary>
/// <seealso cref="BaseProvider"/>
/// <seealso cref="IConfigurationProvider"/>
public abstract class EnvironmentProvider : BaseProvider
{
    /// <inheritdoc cref="BaseProvider.Set{T}(string, T)"/>
    public override void Set<T>(string variable, T value)
    {
        var realValue = value.ToString();

        if (typeof(T).IsArray)
            realValue = string.Join(",", value as Array);

        Environment.SetEnvironmentVariable(variable, realValue);
    }

    /// <inheritdoc cref="BaseProvider.GetRawValue(string, out string)"/>
    protected override bool GetRawValue(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);

        return !string.IsNullOrEmpty(value);
    }
}
