namespace Grid.Bot.Utility;

using System;
using System.Collections.Generic;

using Secrets = System.Collections.Generic.IDictionary<string, object>;

/// <summary>
/// A factory that provides client-usable settings.
/// </summary>
public interface IClientSettingsFactory
{
    /// <summary>
    /// Gets the raw settings from the backed refresh ahead.
    /// </summary>
    IDictionary<string, Secrets> RawSettings { get; }

    /// <summary>
    /// Refreshes the settings.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Gets the settings for the specified application.
    /// </summary>
    /// <param name="application">The name of the application.</param>
    /// <param name="withDependencies">if set to <c>true</c> [with dependencies].</param>
    /// <returns>The settings for the specified application.</returns>
    /// <exception cref="ArgumentException"><paramref name="application"/> is <c>null</c> or whitespace.</exception>
    Secrets GetSettingsForApplication(string application, bool withDependencies = true);

    /// <summary>
    /// Gets the specific setting for the specified application.
    /// </summary>
    /// <typeparam name="T">The type of the setting. Can either be a string, int or bool.</typeparam>
    /// <param name="application">The name of the application.</param>
    /// <param name="setting">The name of the setting.</param>
    /// <param name="withDependencies">if set to <c>true</c> [with dependencies].</param>
    /// <returns>The setting for the specified application.</returns>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="T"/> can only be a string, int or bool.
    /// - <paramref name="application"/> is <c>null</c> or whitespace.
    /// - <paramref name="setting"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">The setting was not found for the specified application.</exception>
    /// <exception cref="InvalidCastException">The setting could not be cast to the specified type.</exception>
    T GetSettingForApplication<T>(string application, string setting, bool withDependencies = true);

    /// <summary>
    /// Gets the specific setting for the specified application.
    /// </summary>
    /// <typeparam name="T">The type of the setting. Can either be a string, int or bool.</typeparam>
    /// <param name="application">The name of the application.</param>
    /// <param name="setting">The name of the setting.</param>
    /// <param name="filterType">The type of filtered value to read.</param>
    /// <param name="withDependencies">if set to <c>true</c> [with dependencies].</param>
    /// <returns>The setting for the specified application.</returns>
    /// <exception cref="ArgumentException">
    /// - <typeparamref name="T"/> can only be a string, int or bool.
    /// - <paramref name="application"/> is <c>null</c> or whitespace.
    /// - <paramref name="setting"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">The setting was not found for the specified application.</exception>
    /// <exception cref="InvalidCastException">The setting could not be cast to the specified type.</exception>
    FilteredValue<T> GetFilteredSettingForApplication<T>(string application, string setting, FilterType filterType = FilterType.Place, bool withDependencies = true);

    /// <summary>
    /// Import settings for the specified application.
    /// </summary>
    /// <remarks>Any non-prefixed settings will have their types placed into metadata.</remarks>
    /// <param name="application">The name of the application</param>
    /// <param name="settings">The raw setting.</param>
    void WriteSettingsForApplication(string application, Secrets settings);

    /// <summary>
    /// Sets the value of the specified setting.
    /// </summary>
    /// <typeparam name="T">The type of the setting. Can either be a string, int, bool or <see cref="FilteredValue{T}"/></typeparam>
    /// <param name="application">The name of the application.</param>
    /// <param name="setting">The name of the setting.</param>
    /// <param name="value">The value of the setting, this can be of type <see cref="FilteredValue{T}"/></param>
    void SetSettingForApplication<T>(string application, string setting, T value);

    /// <summary>
    /// Sets the value of the specified setting.
    /// </summary>
    /// <param name="application">The name of the application.</param>
    /// <param name="setting">The name of the setting.</param>
    /// <param name="value">The value of the setting.</param>
    /// <param name="settingType">The type of the setting.</param>
    /// <exception cref="ArgumentException">
    /// - <paramref name="application"/> is <c>null</c> or whitespace.
    /// - <paramref name="setting"/> is <c>null</c> or whitespace.
    /// - <paramref name="value"/> is <c>null</c> or whitespace.
    /// </exception>
    void SetSettingForApplication(string application, string setting, object value, SettingType settingType = SettingType.String);
}