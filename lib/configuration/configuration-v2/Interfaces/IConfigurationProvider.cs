namespace Configuration;

using System;
using System.ComponentModel;

using Logging;

/// <summary>
/// Represents the implementation of a configuration provder.
/// </summary>
public interface IConfigurationProvider : INotifyPropertyChanged
{
    /// <summary>
    /// Sets the logger for the provider.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    void SetLogger(ILogger logger);

    /// <summary>
    /// Was the specified variable overriden?
    /// </summary>
    /// <param name="variable">The name of the variable.</param>
    /// <returns>True if the variable was overriden, false otherwise.</returns>
    bool IsVariableOverridden(string variable);

    /// <summary>
    /// Gets the value of the overriden variable.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="variable">The name of the variable.</param>
    /// <returns>The type of the value, null if not overriden.</returns>
    T GetOverridenVariable<T>(string variable);

    /// <summary>
    /// Overrides the specified variable.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="variable">The name of the variable.</param>
    /// <param name="value">The value of the variable.</param>
    void OverrideVariable<T>(string variable, T value);

    /// <summary>
    /// Removes the overridden variable.
    /// </summary>
    /// <param name="variable">The name of the variable.</param>
    /// <returns>True if the variable was removed or not.</returns>
    bool RemoveOverridenVariable(string variable);

    /// <summary>
    /// Sets the value for the specified key.
    /// 
    /// Is implementation specific.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="variable">The name of the variable.</param>
    /// <param name="value">The value.</param>
    void Set<T>(string variable, T value);

    /// <summary>
    /// Gets the value specified by the key or returns the default value.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="variable">The name of the variable.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The value or default of <typeparamref name="T"/></returns>
    T GetOrDefault<T>(string variable, T defaultValue = default(T));

    /// <summary>
    /// Gets the value specified by the key or returns the default value.
    /// </summary>
    /// <typeparam name="T">The type of the variable.</typeparam>
    /// <param name="variable">The name of the variable.</param>
    /// <param name="defaultValueGetter">The default value getter.</param>
    /// <returns>The value or default of <typeparamref name="T"/></returns>
    T GetOrDefault<T>(string variable, Func<T> defaultValueGetter);
}
