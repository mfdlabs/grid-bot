namespace Redis;

using System;
using System.ComponentModel;

/// <summary>
/// Settings for the Self Healing Connection Multiplexer
/// </summary>
public interface ISelfHealingConnectionMultiplexerSettings : INotifyPropertyChanged
{
    /// <summary>
    /// Detection interval
    /// </summary>
    TimeSpan DetectionInterval { get; }

    /// <summary>
    /// Threshold for detection.
    /// </summary>
    int DetectionThreshold { get; }

    /// <summary>
    /// Is the feature enabled?
    /// </summary>
    bool FeatureEnabled { get; }

    /// <summary>
    /// Grace period for resets.
    /// </summary>
    TimeSpan ResetGracePeriod { get; }
}
