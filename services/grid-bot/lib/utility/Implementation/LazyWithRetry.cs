namespace Grid.Bot.Utility;

using System;

/// <summary>
/// Lazy with retry implementation
/// </summary>
public class LazyWithRetry<T>
{
    private readonly TimeSpan _TimeoutBetweenRetries = TimeSpan.FromSeconds(30);

    private Lazy<T> _Lazy;

    private readonly object _Sync = new();
    private readonly Func<T> _ValueFactory;
    private readonly Func<DateTime> _NowGetter;

    private DateTime? _LastExceptionTimeStamp;

    /// <inheritdoc cref="Lazy{T}.IsValueCreated" />
    public bool IsValueCreated => _Lazy.IsValueCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyWithRetry{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="nowGetter">The now getter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <see langword="null" />.</exception>
    public LazyWithRetry(Func<T> valueFactory, Func<DateTime> nowGetter = null)
    {
        _ValueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));

        _Lazy = new Lazy<T>(_ValueFactory);
        _NowGetter = nowGetter ?? (() => DateTime.UtcNow);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyWithRetry{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="timeoutBetweenRetries">The timeout between retries.</param>
    /// <param name="nowGetter">The now getter.</param>
    public LazyWithRetry(Func<T> valueFactory, TimeSpan timeoutBetweenRetries, Func<DateTime> nowGetter = null) 
        : this(valueFactory, nowGetter)
    {
        _TimeoutBetweenRetries = timeoutBetweenRetries;
    }

    /// <inheritdoc cref="Lazy{T}.Value" />
    public T LazyValue
    {
        get
        {
            try
            {
                return _Lazy.Value;
            }
            catch (Exception)
            {
                bool isTimeForReset = false;
                lock (_Sync)
                {
                    if (_LastExceptionTimeStamp == null)
                        _LastExceptionTimeStamp = _NowGetter();
                    else
                    {
                        DateTime t = _NowGetter();
                        if (t > _LastExceptionTimeStamp + _TimeoutBetweenRetries)
                        {
                            Reset();
                            isTimeForReset = true;
                        }
                    }
                }
                if (!isTimeForReset)
                    throw;

                return _Lazy.Value;
            }
        }
    }

    /// <summary>
    /// Reset the current lazy value.
    /// </summary>
    public void Reset()
    {
        _Lazy = new Lazy<T>(_ValueFactory);
        _LastExceptionTimeStamp = null;
    }
}