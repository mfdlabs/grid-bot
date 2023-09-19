namespace Configuration;

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using Logging;

#nullable enable

/// <summary>
/// Extensions for Configuration.
/// </summary>
public static class Extensions
{
    // language=regex
    private const string _envVarRegex = @"^\$\{\{\s*env\.(?<env_var_name>\w+)\s*\}\}$";

    internal static TResult? To<TResult>(this object obj) => (TResult?)obj.To(typeof(TResult));
    internal static object? To(this object obj, Type t) => Convert.ChangeType(obj, t);
    internal static string GetPropertyName<TSettingsProvider, TValue>(this Expression<Func<TSettingsProvider, TValue>> expressuib)
        where TSettingsProvider : INotifyPropertyChanged
        => ((MemberExpression)expressuib.Body).Member.Name;


    internal static void SafelyInvoke(this Action propertyChangeHandler, ILogger? logger)
    {
        try
        {
            propertyChangeHandler();
        }
        catch (Exception ex)
        {
            logger?.Error("{0}", ex.ToString());
        }
    }

    internal static void SafelyInvoke<TVal>(this Action<TVal> propertyChangeHandler, TVal propertyValue, ILogger? logger)
    {
        try
        {
            propertyChangeHandler(propertyValue);
        }
        catch (Exception ex)
        {
            logger?.Error(ex);
        }
    }

    /// <summary>
    /// Reads the specified object as a string in the form of:<br />
    /// <code>^\$\{\{\s*env\.(?&lt;env_var_name&gt;\w+)\s*\}\}$</code><br />
    /// 
    /// And then fetches the env_var_name from the environment variables.
    /// </summary>
    /// <remarks>
    ///     The way this works is that the input MUST be a string, but is treated as
    ///     an object for the case of "unknown" settings from providers and such.
    /// </remarks>
    /// <typeparam name="TResult">The type of the actual value.</typeparam>
    /// <param name="setting">The input setting.</param>
    /// <returns>The <typeparamref name="TResult"/> or null.</returns>
    public static TResult? FromEnvironmentExpression<TResult>(this object setting)
    {
        if (setting is not string str) return default;
        if (string.IsNullOrEmpty(str)) return str.To<TResult>();

        var match = Regex.Match(str, _envVarRegex);
        if (!match.Success) return str.To<TResult>();

        var envVarName = match.Groups["env_var_name"];
        if (envVarName == null) return str.To<TResult>();

        return Environment.GetEnvironmentVariable(envVarName.Value).To<TResult>();
    }

    /// <summary>
    /// Converts a setting from <typeparamref name="TSettingsProvider"/> to a <see cref="ISingleSetting{T}"/>.
    /// </summary>
    /// <typeparam name="TSettingsProvider">The type of the settings. Can be anything that is <see cref="INotifyPropertyChanged"/></typeparam>
    /// <typeparam name="TValue">The type of the value to pass to the <see cref="ISingleSetting{T}"/></typeparam>
    /// <param name="provider">The <typeparamref name="TSettingsProvider"/></param>
    /// <param name="expression">The <see cref="Expression"/> that evaluates on every read of the <see cref="ISingleSetting{T}"/></param>
    /// <param name="logger">An optional <see cref="ILogger"/> to use.</param>
    /// <returns>The <see cref="ISingleSetting{T}"/></returns>
    public static ISingleSetting<TValue> ToSingleSetting<TSettingsProvider, TValue>(
        this TSettingsProvider provider, 
        Expression<Func<TSettingsProvider, TValue>> expression,
        ILogger? logger = null
    )
        where TSettingsProvider : class, INotifyPropertyChanged
        => new SingleSetting<TSettingsProvider, TValue>(
            provider,
            expression.Compile(),
            expression.GetPropertyName(),
            logger
        );

    /// <summary>
    /// Reads the value of the <see cref="Expression"/> from <typeparamref name="TSettingsProvider"/> and monitors it for future changes.
    /// </summary>
    /// <typeparam name="TSettingsProvider">The type of the settings. Can be anything that is <see cref="INotifyPropertyChanged"/></typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="provider">The <typeparamref name="TSettingsProvider"/></param>
    /// <param name="expression">The <see cref="Expression"/> to evaluate.</param>
    /// <param name="propertyChangeHandler">The handler to be invoked on <see cref="INotifyPropertyChanged.PropertyChanged"/></param>
    /// <param name="logger">An optional <see cref="ILogger"/> to use.</param>
    public static void ReadValueAndMonitorChanges<TSettingsProvider, TValue>(
        this TSettingsProvider provider,
        Expression<Func<TSettingsProvider, TValue>> expression,
        Action propertyChangeHandler, 
        ILogger? logger = null
    )
        where TSettingsProvider : INotifyPropertyChanged
        => provider.MonitorChanges(
            expression,
            propertyChangeHandler,
            true, 
            logger
        );

    /// <summary>
    /// Monitors <typeparamref name="TSettingsProvider"/> future changes.
    /// </summary>
    /// <typeparam name="TSettingsProvider">The type of the settings. Can be anything that is <see cref="INotifyPropertyChanged"/></typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="provider">The <typeparamref name="TSettingsProvider"/></param>
    /// <param name="expression">The <see cref="Expression"/> to evaluate.</param>
    /// <param name="propertyChangeHandler">The handler to be invoked on <see cref="INotifyPropertyChanged.PropertyChanged"/></param>
    /// <param name="logger">An optional <see cref="ILogger"/> to use.</param>
    public static void MonitorChanges<TSettingsProvider, TValue>(
        this TSettingsProvider provider,
        Expression<Func<TSettingsProvider, TValue>> expression,
        Action propertyChangeHandler, 
        ILogger? logger = null
    )
        where TSettingsProvider : INotifyPropertyChanged
        => provider.MonitorChanges(
            expression,
            propertyChangeHandler,
            false, logger
        );

    private static void MonitorChanges<TSettingsProvider, TValue>(
        this TSettingsProvider provider,
        Expression<Func<TSettingsProvider, TValue>> expression,
        Action propertyChangeHandler, 
        bool readValueFirst, 
        ILogger? logger = null
    )
        where TSettingsProvider : INotifyPropertyChanged
    {
        var getter = expression.Compile();
        var value = getter(provider);

        if (readValueFirst) propertyChangeHandler.SafelyInvoke(logger);

        var name = expression.GetPropertyName();
        provider.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == name)
            {
                var newValue = getter(provider);

                if (newValue == null || !newValue.Equals(value))
                {
                    propertyChangeHandler.SafelyInvoke(logger);

                    value = newValue;
                }
            }
        };
    }

    /// <summary>
    /// Reads the value of the <see cref="Expression"/> from <typeparamref name="TSettingsProvider"/> and monitors it for future changes.
    /// </summary>
    /// <typeparam name="TSettingsProvider">The type of the settings. Can be anything that is <see cref="INotifyPropertyChanged"/></typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="provider">The <typeparamref name="TSettingsProvider"/></param>
    /// <param name="expression">The <see cref="Expression"/> to evaluate.</param>
    /// <param name="propertyChangeHandler">The handler to be invoked on <see cref="INotifyPropertyChanged.PropertyChanged"/></param>
    /// <param name="logger">An optional <see cref="ILogger"/> to use.</param>
    public static void ReadValueAndMonitorChanges<TSettingsProvider, TValue>(
        this TSettingsProvider provider,
        Expression<Func<TSettingsProvider, TValue>> expression, 
        Action<TValue> propertyChangeHandler, 
        ILogger? logger = null
    )
        where TSettingsProvider : INotifyPropertyChanged
        => provider.MonitorChanges(
            expression, 
            propertyChangeHandler,
            true,
            logger
        );

    /// <summary>
    /// Monitors <typeparamref name="TSettingsProvider"/> future changes.
    /// </summary>
    /// <typeparam name="TSettingsProvider">The type of the settings. Can be anything that is <see cref="INotifyPropertyChanged"/></typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="provider">The <typeparamref name="TSettingsProvider"/></param>
    /// <param name="expression">The <see cref="Expression"/> to evaluate.</param>
    /// <param name="propertyChangeHandler">The handler to be invoked on <see cref="INotifyPropertyChanged.PropertyChanged"/></param>
    /// <param name="logger">An optional <see cref="ILogger"/> to use.</param>
    public static void MonitorChanges<TSettingsProvider, TValue>(
        this TSettingsProvider provider, 
        Expression<Func<TSettingsProvider, TValue>> expression,
        Action<TValue> propertyChangeHandler,
        ILogger? logger = null
    )
        where TSettingsProvider : INotifyPropertyChanged
        => provider.MonitorChanges(
            expression, 
            propertyChangeHandler,
            false, 
            logger
        );


    private static void MonitorChanges<TSettingsProvider, TValue>(
        this TSettingsProvider provider,
        Expression<Func<TSettingsProvider, TValue>> expression,
        Action<TValue> propertyChangeHandler,
        bool readValueFirst, 
        ILogger? logger = null
    )
        where TSettingsProvider : INotifyPropertyChanged
    {
        var getter = expression.Compile();
        var value = getter(provider);

        if (readValueFirst) propertyChangeHandler.SafelyInvoke(value, logger);

        var name = GetPropertyName(expression);
        provider.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == name)
            {
                var newValue = getter(provider);

                if (newValue == null || !newValue.Equals(value))
                {
                    propertyChangeHandler.SafelyInvoke(newValue, logger);

                    value = newValue;
                }
            }
        };
    }


}
