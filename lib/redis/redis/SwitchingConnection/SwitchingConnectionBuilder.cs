namespace Redis;

using StackExchange.Redis;
using System;
using System.Threading.Tasks;

/// <summary>
/// Builder for a switching connection multiplexer.
/// </summary>
public partial class SwitchingConnectionBuilder : IConnectionBuilder
{
    private bool _UseSecond;

    private readonly IConnectionBuilder _FirstConnectionBuilder;
    private readonly IConnectionBuilder _SecondConnectionBuilder;

    private event Action<bool> SwitchSet;

    /// <summary>
    /// Construct a new instance of <see cref="SwitchingConnectionBuilder"/>
    /// </summary>
    /// <param name="firstConnectionBuilder">The first connection builder.</param>
    /// <param name="secondConnectionBuilder">The second connection builder.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="firstConnectionBuilder"/> cannot be null.
    /// - <paramref name="secondConnectionBuilder"/> cannot be null.
    /// </exception>
    public SwitchingConnectionBuilder(IConnectionBuilder firstConnectionBuilder, IConnectionBuilder secondConnectionBuilder)
    {
        _FirstConnectionBuilder = firstConnectionBuilder ?? throw new ArgumentNullException(nameof(firstConnectionBuilder));
        _SecondConnectionBuilder = secondConnectionBuilder ?? throw new ArgumentNullException(nameof(secondConnectionBuilder));
    }

    /// <summary>
    /// Set the switch to either connection builder.
    /// </summary>
    /// <param name="useSecond">The value</param>
    public void SetSwitch(bool useSecond)
    {
        if (_UseSecond == useSecond) return;

        _UseSecond = useSecond;

        SwitchSet?.Invoke(_UseSecond);
    }

    /// <inheritdoc cref="IConnectionBuilder.CreateConnectionMultiplexerAsync(ConfigurationOptions)"/>
    public async Task<IConnectionMultiplexer> CreateConnectionMultiplexerAsync(ConfigurationOptions configuration)
    {
        IConnectionMultiplexer initialMultiplexer;

        if (_UseSecond)
            initialMultiplexer = await _SecondConnectionBuilder.CreateConnectionMultiplexerAsync(configuration).ConfigureAwait(false);
        else
            initialMultiplexer = await _FirstConnectionBuilder.CreateConnectionMultiplexerAsync(configuration).ConfigureAwait(false);
        
        var multiplexer = new SwitchingConnectionMultiplexer(
            initialMultiplexer, 
            _FirstConnectionBuilder, 
            _SecondConnectionBuilder, 
            _UseSecond, 
            configuration
        );
        SwitchSet += multiplexer.SetSwitch;

        return multiplexer;
    }
}
