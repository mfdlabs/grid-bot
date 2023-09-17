namespace Random;

using System;

/// <inheritdoc cref="IPercentageInvoker"/>
public class PercentageInvoker : IPercentageInvoker
{
    private static IPercentageInvoker _singleton;
    private static readonly object _lock = new();

    /// <summary>
    /// Get the Singleton instance of <see cref="PercentageInvoker"/>
    /// </summary>
    public static IPercentageInvoker Singleton
    {
        get
        {
            lock (_lock)
                return _singleton ??= new PercentageInvoker(RandomFactory.GetDefaultRandom());
        }
    }

    private readonly IRandom _random;

    /// <summary>
    /// Construct a new instance of <see cref="PercentageInvoker"/>
    /// </summary>
    /// <param name="random">The <see cref="IRandom"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="random"/> cannot be null.</exception>
    public PercentageInvoker(IRandom random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <inheritdoc cref="IPercentageInvoker.InvokeAction(Action, int)"/>
    public bool InvokeAction(Action action, int invokePercentage)
    {
        if (invokePercentage < 0 || invokePercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(invokePercentage), "invokePercentage must be between 0 and 100.");

        if (CanInvoke(invokePercentage))
        {
            action();
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="IPercentageInvoker.CanInvoke(int)"/>
    public bool CanInvoke(int invokePercentage)
    {
        if (invokePercentage < 0 || invokePercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(invokePercentage), "invokePercentage must be between 0 and 100.");

        return _random.Next() % 100 < invokePercentage;
    }

}
