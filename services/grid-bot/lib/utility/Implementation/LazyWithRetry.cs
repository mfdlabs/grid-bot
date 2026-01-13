namespace Grid.Bot.Utility;

using System;

/// <summary>
/// Lazy with retry implementation
/// </summary>
public class LazyWithRetry<T>
{
    private readonly TimeSpan _timeoutBetweenRetries = TimeSpan.FromSeconds(30);

    private Lazy<T> _lazy;

    private readonly object _sync = new();
    private readonly Func<T> _valueFactory;
    private readonly Func<DateTime> _nowGetter;

    private DateTime? _lastExceptionTimeStamp;

    /// <inheritdoc cref="Lazy{T}.IsValueCreated" />
    public bool IsValueCreated => _lazy.IsValueCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyWithRetry{T}"/> class.
    /// </summary>
    /// <param name="valueFactory">The value factory.</param>
    /// <param name="nowGetter">The now getter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <see langword="null" />.</exception>
    public LazyWithRetry(Func<T> valueFactory, Func<DateTime> nowGetter = null)
    {
        _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));

        _lazy = new Lazy<T>(_valueFactory);
        _nowGetter = nowGetter ?? (() => DateTime.UtcNow);
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
        _timeoutBetweenRetries = timeoutBetweenRetries;
    }

    /// <inheritdoc cref="Lazy{T}.Value" />
    public T LazyValue
    {
        get
        {
            try
            {
                return _lazy.Value;
            }
            catch (Exception)
            {
                var isTimeForReset = false;
                lock (_sync)
                {
                    if (_lastExceptionTimeStamp == null)
                        _lastExceptionTimeStamp = _nowGetter();
                    else
                    {
                        var t = _nowGetter();
                        if (t > _lastExceptionTimeStamp + _timeoutBetweenRetries)
                        {
                            Reset();
                            isTimeForReset = true;
                        }
                    }
                }
                if (!isTimeForReset)
                    throw;

                return _lazy.Value;
            }
        }
    }

    /// <summary>
    /// Reset the current lazy value.
    /// </summary>
    public void Reset()
    {
        _lazy = new Lazy<T>(_valueFactory);
        _lastExceptionTimeStamp = null;
    }
}
