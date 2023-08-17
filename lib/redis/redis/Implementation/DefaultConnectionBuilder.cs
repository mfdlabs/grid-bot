namespace Redis;

using System.Threading.Tasks;

using StackExchange.Redis;

/// <summary>
/// Default implementation for <see cref="IConnectionBuilder"/>
/// </summary>
public class DefaultConnectionBuilder : IConnectionBuilder
{
    /// <inheritdoc cref="IConnectionBuilder.CreateConnectionMultiplexerAsync(ConfigurationOptions)"/>
    public async Task<IConnectionMultiplexer> CreateConnectionMultiplexerAsync(ConfigurationOptions configurationOptions) 
        => await ConnectionMultiplexer.ConnectAsync(configurationOptions).ConfigureAwait(false);

}
