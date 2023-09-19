namespace Configuration;

using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Logging;

/// <summary>
/// Base implementation for <see cref="IConfigurationProvider"/>
/// </summary>
/// <seealso cref="IConfigurationProvider"/>
public abstract class BaseProvider : IConfigurationProvider
{
    private readonly ConcurrentDictionary<string, object> _overriddenVariables = new();

    /// <summary>
    /// Exposes the logger for this provider.
    /// </summary>
    protected ILogger _logger;

    /// <summary>
    /// Construct a new instance of <see cref="BaseProvider"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    protected BaseProvider(ILogger logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public virtual event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc cref="IConfigurationProvider.SetLogger(ILogger)"/>
    public void SetLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc cref="IConfigurationProvider.GetOverridenVariable{T}(string)"/>
    public virtual T GetOverridenVariable<T>(string variable)
    {
        if (!IsVariableOverridden(variable)) return default(T);
        if (!_overriddenVariables.TryGetValue(variable, out var value)) return default(T);

        return value.To<T>();
    }

    /// <inheritdoc cref="IConfigurationProvider.IsVariableOverridden(string)"/>
    public virtual bool IsVariableOverridden(string variable) => _overriddenVariables.ContainsKey(variable);

    /// <inheritdoc cref="IConfigurationProvider.OverrideVariable{T}(string, T)"/>
    public virtual void OverrideVariable<T>(string variable, T value)
    {
        _overriddenVariables.AddOrUpdate(variable, value, (_, _) => value);

        _logger?.Debug("Overrode variable '{0}'!", variable);

        PropertyChanged?.Invoke(this, new(variable));
    }

    /// <inheritdoc cref="IConfigurationProvider.RemoveOverridenVariable(string)"/>
    public virtual bool RemoveOverridenVariable(string variable)
    {
        var success = _overriddenVariables.TryRemove(variable, out _);

        _logger?.Debug("Removed overridden variable '{0}'", variable);

        if (success)
            PropertyChanged?.Invoke(this, new(variable));

        return success;
    }

    /// <summary>
    /// Get the actual value for the key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>True if the value was found, false otherwise.</returns>
    protected abstract bool GetRawValue(string key, out string value);

    /// <summary>
    /// Converts the specified value to the specified type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The <see cref="Type"/></param>
    /// <returns>The converted value.</returns>
    protected object ConvertTo(string value, Type type)
    {
        if (string.IsNullOrEmpty(value)) return default;

        if (type.IsArray)
        {
            type = type.GetElementType();

            var v = value.Split(',');
            var arr = new List<object>();

            bool allNullValues = true;

            foreach (var e in v)
            {
                if (string.IsNullOrEmpty(e)) continue;

                var val = ConvertTo(e, type);

                if (val != null)
                {
                    allNullValues = false;

                    arr.Add(val);
                }
            }

            if (allNullValues) return null;

            return arr.ToArray();
        }

        if (type.IsEnum)
        {
            if (!TryParseEnum(value, type, false, out var @enum))
            {
                _logger?.Error("Failed to parse value to enum type of '{0}'", type.FullName);

                return null;
            }

            return @enum;
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.Parse(value);
        }

        var attribute = type.GetCustomAttribute<TypeConverterAttribute>();
        if (attribute != null)
        {
            var converterType = Type.GetType(attribute.ConverterTypeName);
            if (converterType != null)
            {
                var converter = (TypeConverter)converterType.GetConstructor(Array.Empty<Type>())?.Invoke(Array.Empty<object>());
                if (converter != null)
                {
                    if (converter.CanConvertFrom(typeof(string)))
                        return converter.ConvertFrom(value);
                }
            }
        }

        try
        {
            return value.To(type);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseEnum(string value, Type enumType, bool ignoreCase, out Enum enumValue)
    {
        if (Enum.IsDefined(enumType, value))
        {
            enumValue = (Enum)Enum.Parse(enumType, value, ignoreCase);

            return true;
        }

        enumValue = null;

        return false;
    }

    /// <inheritdoc cref="IConfigurationProvider.Set{T}(string, T)"/>
    public abstract void Set<T>(string variable, T value);

    /// <inheritdoc cref="IConfigurationProvider.GetOrDefault{T}(string, T)"/>
    public T GetOrDefault<T>(string variable, T defaultValue = default(T)) => GetOrDefault(variable, () => defaultValue);

    /// <inheritdoc cref="IConfigurationProvider.GetOrDefault{T}(string, Func{T})"/>
    public T GetOrDefault<T>(string variable, Func<T> defaultValueGetter)
    {
        if (IsVariableOverridden(variable)) return GetOverridenVariable<T>(variable);
        if (!GetRawValue(variable, out var value)) return defaultValueGetter();

        return (T)ConvertTo(value, typeof(T)) ?? defaultValueGetter();
    }
}
