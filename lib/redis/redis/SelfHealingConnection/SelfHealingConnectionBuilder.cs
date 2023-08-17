namespace Redis;

using System;
using System.Threading.Tasks;

using StackExchange.Redis;

/// <summary>
/// Connection builder for a self healing connection multiplexer.
/// </summary>
public partial class SelfHealingConnectionBuilder : IConnectionBuilder
{
    private readonly IConnectionBuilder _WrappedConnectionBuilder;
    private readonly ISelfHealingConnectionMultiplexerSettings _Settings;
    private readonly Func<DateTime> _GetCurrentTimeFunc;

    /// <summary>
    /// Construct a new instance of <see cref="SelfHealingConnectionBuilder"/>
    /// </summary>
    /// <param name="wrappedConnectionBuilder">The wrapped connection multiplexer builder.</param>
    /// <param name="settings">The SHCM settings.</param>
    /// <param name="getCurrentTimeFunc">Return the current time.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="wrappedConnectionBuilder"/> cannot be null.
    /// - <paramref name="settings"/> cannot be null.
    /// - <paramref name="getCurrentTimeFunc"/> cannot be null.
    /// </exception>
    public SelfHealingConnectionBuilder(IConnectionBuilder wrappedConnectionBuilder, ISelfHealingConnectionMultiplexerSettings settings, Func<DateTime> getCurrentTimeFunc)
    {
        _WrappedConnectionBuilder = wrappedConnectionBuilder ?? throw new ArgumentNullException(nameof(wrappedConnectionBuilder));
        _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _GetCurrentTimeFunc = getCurrentTimeFunc ?? throw new ArgumentNullException(nameof(getCurrentTimeFunc));
    }

    /// <inheritdoc cref="IConnectionBuilder.CreateConnectionMultiplexerAsync(ConfigurationOptions)"/>
    public async Task<IConnectionMultiplexer> CreateConnectionMultiplexerAsync(ConfigurationOptions configuration)
    {
        return new SelfHealingConnectionMultiplexer(
            await _WrappedConnectionBuilder.CreateConnectionMultiplexerAsync(configuration).ConfigureAwait(false), 
            _WrappedConnectionBuilder, 
            configuration, 
            _Settings, 
            _GetCurrentTimeFunc
        );
    }
}
