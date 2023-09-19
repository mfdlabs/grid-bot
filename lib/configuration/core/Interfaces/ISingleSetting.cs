namespace Configuration;

using System.ComponentModel;

/// <summary>
/// Represents a monitored setting.
/// </summary>
/// <typeparam name="T">The type of the setting.</typeparam>
public interface ISingleSetting<T> : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the value of the setting.
    /// </summary>
    T Value { get; }
}
