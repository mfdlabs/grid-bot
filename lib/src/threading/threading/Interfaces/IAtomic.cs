namespace Threading;

using System;

/// <summary>
/// Represents an atomically usable number.
/// </summary>
/// <typeparam name="T">The type of the number</typeparam>
public interface IAtomic<T> :
    IComparable,
    IFormattable,
    IConvertible,
    IComparable<IAtomic<T>>,
    IEquatable<IAtomic<T>>
where T : 
    struct, 
    IComparable, 
    IFormattable,
    IConvertible, 
    IComparable<T>,
    IEquatable<T>
{
    /// <summary>
    /// Gets the current value of the <see cref="IAtomic{T}"/>
    /// </summary>
    T Value { get; set; }

    /// <summary>
    /// Compare and swap the atomic.
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="comparand">The value to compare</param>
    /// <returns>The value.</returns>
    T CompareAndSwap(T value, T comparand);

    /// <summary>
    /// Swap the atomic
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The new value.</returns>
    T Swap(T value);
}
