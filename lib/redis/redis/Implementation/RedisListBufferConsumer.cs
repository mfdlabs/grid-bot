namespace Redis;

using System;
using System.ComponentModel;
using System.Threading.Tasks;

using StackExchange.Redis;

using Logging;

/// <summary>
/// Buffered request consumer.
/// </summary>
public class RedisListBufferConsumer
{
    private readonly IRedisListConfigurationBuffer _RedisConfigurationBuffer;
    private readonly Func<bool> _IsSenderEnabledGetter;
    private readonly ILogger _BackupLogger;

    private ConfigurationOptions _ConfigurationOptions;
    internal IConnectionMultiplexer Redis;

    /// <summary>
    /// Construct a new insance of <see cref="RedisListBufferConsumer"/>
    /// </summary>
    /// <param name="isSenderEnabledGetter">A function that determines if the sender is enabled.</param>
    /// <param name="backupLogger">The <see cref="ILogger"/></param>
    /// <param name="settings">The settings.</param>
    /// <param name="redisConfigurationBuffer">The <see cref="IRedisListConfigurationBuffer"/></param>
    public RedisListBufferConsumer(
        Func<bool> isSenderEnabledGetter, 
        ILogger backupLogger, 
        INotifyPropertyChanged settings,
        IRedisListConfigurationBuffer redisConfigurationBuffer
    )
    {
        _RedisConfigurationBuffer = redisConfigurationBuffer;
        _IsSenderEnabledGetter = isSenderEnabledGetter;
        _BackupLogger = backupLogger;

        settings.PropertyChanged += SettingsOnPropertyChanged;

        _ConfigurationOptions = _RedisConfigurationBuffer.GetConfigurationOptions();

        Redis = ConnectionMultiplexer.Connect(_ConfigurationOptions);
    }

    private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        if (!_RedisConfigurationBuffer.NeedsReCreation(propertyChangedEventArgs.PropertyName))
            return;

        if (_IsSenderEnabledGetter())
        {
            _BackupLogger.Error("Changes to RedisListBufferSender while Sender is enabled are not possible. First disable Sender.");
            return;
        }

        try
        {
            Redis?.Dispose();
            _ConfigurationOptions = _RedisConfigurationBuffer.GetConfigurationOptions();

            Redis = ConnectionMultiplexer.Connect(_ConfigurationOptions);
            if (!Redis.IsConnected)
                _BackupLogger.Error("Redis was not able to connect after \"detecting\" a change to property: {0}", propertyChangedEventArgs.PropertyName);
        }
        catch (Exception ex)
        {
            _BackupLogger.Error("There was an exception while trying to change Redis Configuration at runtime: {0}", ex);
        }
    }

    /// <summary>
    /// Send a buffer to Redis.
    /// </summary>
    /// <param name="buffer">The buffer</param>
    /// <returns>An awaitable task.</returns>
    public async Task SendAsync(string buffer)
    {
        if (!_IsSenderEnabledGetter()) return;

        if (Redis == null || !Redis.IsConnected)
            _BackupLogger.Error("Redis Buffer Sender is disconnected, the following message was not sent: {0}", buffer);
        else
        {
            try
            {
                await Redis.GetDatabase().ListLeftPushAsync(_RedisConfigurationBuffer.RedisListKey, buffer).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _BackupLogger.Error("Redis Buffer Sender failed to send message: {0}  -- Exception: {1}", buffer, ex);
            }
        }
    }
}
