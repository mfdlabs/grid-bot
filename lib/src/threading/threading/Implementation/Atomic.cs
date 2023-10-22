namespace Threading;

using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <inheritdoc cref="IAtomic{T}"/>
[Serializable]
[ComVisible(true)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public struct Atomic<T> :
    IAtomic<T>,
    IComparable,
    IFormattable,
    IConvertible,
    IComparable<IAtomic<T>>,
    IEquatable<IAtomic<T>>,
    IComparable<Atomic<T>>,
    IEquatable<Atomic<T>>
where T :
    struct,
    IComparable,
    IFormattable,
    IConvertible,
    IComparable<T>,
    IEquatable<T>
{
    #region |Private Members|

    private dynamic _value;
    private readonly ReaderWriterLockSlim _lock = new();

    #endregion |Private Members|

    #region |Public Members|

    /// <inheritdoc cref="IAtomic{T}.Value"/>
    public T Value
    {
        readonly get
        {
            try
            {
                _lock.EnterReadLock();

                return _value;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        set
        {
            try
            {
                _lock.EnterWriteLock();

                _value = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

        }
    }

    #endregion |Public Members|

    #region |Constructors|

    /// <summary>
    /// Construct a new instance of <see cref="Atomic{T}"/>
    /// </summary>
    public Atomic() => this.Value = (T)(dynamic)0;

    /// <summary>
    /// Construct a new instance of <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="value">The value.</param>
    public Atomic(T value) => this.Value = value;

    /// <summary>
    /// Construct a new instance of <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="other">A <see cref="Atomic{T}"/></param>
    public Atomic(Atomic<T> other) => this.Value = other.Value;

    /// <summary>
    /// Construct a new instance of <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="other">A <see cref="IAtomic{T}"/></param>
    public Atomic(IAtomic<T> other) => this.Value = other.Value;

    #endregion |Constructors|

    #region |Operators|

    #region |Equality|

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <param name="obj">The other <see cref="Atomic{T}"/></param>
    /// <returns>True if equal.</returns>
    public static bool operator ==(Atomic<T> self, Atomic<T> obj) => EqualityComparer<T>.Default.Equals(self.Value, obj.Value);

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <param name="obj">The other <see cref="Atomic{T}"/></param>
    /// <returns>True if not equal.</returns>
    public static bool operator !=(Atomic<T> self, Atomic<T> obj) => !EqualityComparer<T>.Default.Equals(self.Value, obj.Value);

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="IAtomic{T}"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <param name="obj">The other <see cref="IAtomic{T}"/></param>
    /// <returns>True if equal.</returns>
    public static bool operator ==(Atomic<T> self, IAtomic<T> obj) => EqualityComparer<T>.Default.Equals(self.Value, obj.Value);

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="IAtomic{T}"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <param name="obj">The other <see cref="IAtomic{T}"/></param>
    /// <returns>True if not equal.</returns>
    public static bool operator !=(Atomic<T> self, IAtomic<T> obj) => !EqualityComparer<T>.Default.Equals(self.Value, obj.Value);

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <param name="obj">The other <typeparamref name="T"/></param>
    /// <returns>True if equal.</returns>
    public static bool operator ==(Atomic<T> self, T obj) => EqualityComparer<T>.Default.Equals(self.Value, obj);

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <param name="obj">The other <typeparamref name="T"/></param>
    /// <returns>True if not equal.</returns>
    public static bool operator !=(Atomic<T> self, T obj) => !EqualityComparer<T>.Default.Equals(self.Value, obj);

    #endregion |Equality|

    #region |Less/Greater Than|

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="left">The current <see cref="Atomic{T}"/></param>
    /// <param name="right">The other <see cref="Atomic{T}"/></param>
    /// <returns>True if less than.</returns>
    public static bool operator <(Atomic<T> left, Atomic<T> right) => left.Value.CompareTo(right.Value) < 0;

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="left">The current <see cref="Atomic{T}"/></param>
    /// <param name="right">The other <see cref="Atomic{T}"/></param>
    /// <returns>True if greater than.</returns>
    public static bool operator >(Atomic<T> left, Atomic<T> right) => left.Value.CompareTo(right.Value) > 0;

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="IAtomic{T}"/>
    /// </summary>
    /// <param name="left">The current <see cref="Atomic{T}"/></param>
    /// <param name="right">The other <see cref="IAtomic{T}"/></param>
    /// <returns>True if less than.</returns>
    public static bool operator <(Atomic<T> left, IAtomic<T> right) => left.Value.CompareTo(right.Value) < 0;

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <see cref="IAtomic{T}"/>
    /// </summary>
    /// <param name="left">The current <see cref="Atomic{T}"/></param>
    /// <param name="right">The other <see cref="IAtomic{T}"/></param>
    /// <returns>True if greater than.</returns>
    public static bool operator >(Atomic<T> left, IAtomic<T> right) => left.Value.CompareTo(right.Value) > 0;

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="left">The current <see cref="Atomic{T}"/></param>
    /// <param name="right">The other <typeparamref name="T"/></param>
    /// <returns>True if less than.</returns>
    public static bool operator <(Atomic<T> left, T right) => left.Value.CompareTo(right) < 0;

    /// <summary>
    /// Compare <see cref="Atomic{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="left">The current <see cref="Atomic{T}"/></param>
    /// <param name="right">The other <typeparamref name="T"/></param>
    /// <returns>True if greater than.</returns>
    public static bool operator >(Atomic<T> left, T right) => left.Value.CompareTo(right) > 0;

    #endregion |Less/Greater Than|

    #endregion |Operators|

    #region |Implicit Cast Operators|

    /// <summary>
    /// Convert the current <see cref="Atomic{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="self">The <see cref="Atomic{T}"/></param>
    public static implicit operator T(Atomic<T> self) => self.Value;

    /// <summary>
    /// Convert a <typeparamref name="T"/> to <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="v">The <typeparamref name="T"/></param>
    public static implicit operator Atomic<T>(T v) => new(v);

    #endregion |Implicit Cast Operators|

    #region |Compare And Swap|

    /// <inheritdoc cref="IAtomic{T}.CompareAndSwap(T, T)"/>
    public T CompareAndSwap(T value, T comparand) => (T)Interlocked.CompareExchange(ref this._value, value, comparand);

    #endregion |Compare And Swap|

    #region |Swap|

    /// <inheritdoc cref="IAtomic{T}.Swap(T)"/>
    public T Swap(T value) => (T)Interlocked.Exchange(ref this._value, value);

    #endregion |Swap|

    #region |Arithmetic Operators|

    /// <summary>
    /// Increments the current <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <returns>The newly incremeted <see cref="Atomic{T}"/></returns>
    public static Atomic<T> operator ++(Atomic<T> self)
    {
        dynamic v = self.Value;

        return self.Value = ++v;
    }

    /// <summary>
    /// Decrements the current <see cref="Atomic{T}"/>
    /// </summary>
    /// <param name="self">The current <see cref="Atomic{T}"/></param>
    /// <returns>The newly decremented <see cref="Atomic{T}"/></returns>
    public static Atomic<T> operator --(Atomic<T> self)
    {
        dynamic v = self.Value;

        return self.Value = --v;
    }

    #endregion |Arithmetic Operators|

    #region |IComparable Members|

    /// <inheritdoc cref="IComparable.CompareTo(object)"/>
    public int CompareTo(object obj)
    {
        if (obj == null) return 1;

        if (obj is Atomic<T> at)
        {
            if (this < at) return -1;
            if (this > at) return 1;
            return 0;
        }

        throw new ArgumentException("obj must be of type Atomic.");
    }

    /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
    public int CompareTo(IAtomic<T> obj)
    {
        if (this < obj) return -1;
        if (this > obj) return 1;
        return 0;
    }

    /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
    public int CompareTo(Atomic<T> obj)
    {
        if (this < obj) return -1;
        if (this > obj) return 1;
        return 0;
    }

    #endregion |IComparable Members|

    #region |IEquatable Members|

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object obj) => obj is Atomic<T> atomic && this == atomic;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(Atomic<T> atomic) => this == atomic;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(IAtomic<T> atomic) => this == atomic;

    #endregion |IEquatable Members|

    #region |IFormattable Members|

    /// <inheritdoc cref="object.ToString"/>
    public override string ToString() => Value.ToString();

    /// <inheritdoc cref="IFormattable.ToString(string, IFormatProvider)"/>
    public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);

    #endregion |IFormattable Members|

    #region |IConvertible Members|

    /// <inheritdoc cref="IConvertible.GetTypeCode"/>
    public TypeCode GetTypeCode() => this.Value.GetTypeCode();

    /// <inheritdoc cref="IConvertible.ToBoolean(IFormatProvider)"/>
    bool IConvertible.ToBoolean(IFormatProvider provider) => Convert.ToBoolean(this.Value);

    /// <inheritdoc cref="IConvertible.ToChar(IFormatProvider)"/>
    char IConvertible.ToChar(IFormatProvider provider) => Convert.ToChar(this.Value);

    /// <inheritdoc cref="IConvertible.ToSByte(IFormatProvider)"/>
    sbyte IConvertible.ToSByte(IFormatProvider provider) => Convert.ToSByte(this.Value);

    /// <inheritdoc cref="IConvertible.ToByte(IFormatProvider)"/>
    byte IConvertible.ToByte(IFormatProvider provider) => Convert.ToByte(this.Value);

    /// <inheritdoc cref="IConvertible.ToInt16(IFormatProvider)"/>
    short IConvertible.ToInt16(IFormatProvider provider) => Convert.ToInt16(this.Value);

    /// <inheritdoc cref="IConvertible.ToUInt16(IFormatProvider)"/>
    ushort IConvertible.ToUInt16(IFormatProvider provider) => Convert.ToUInt16(this.Value);

    /// <inheritdoc cref="IConvertible.ToInt32(IFormatProvider)"/>
    int IConvertible.ToInt32(IFormatProvider provider) => Convert.ToInt32(this.Value);

    /// <inheritdoc cref="IConvertible.ToUInt32(IFormatProvider)"/>
    uint IConvertible.ToUInt32(IFormatProvider provider) => Convert.ToUInt32(this.Value);

    /// <inheritdoc cref="IConvertible.ToInt64(IFormatProvider)"/>
    long IConvertible.ToInt64(IFormatProvider provider) => Convert.ToInt64(this.Value);

    /// <inheritdoc cref="IConvertible.ToUInt64(IFormatProvider)"/>
    ulong IConvertible.ToUInt64(IFormatProvider provider) => Convert.ToUInt64(this.Value);

    /// <inheritdoc cref="IConvertible.ToSingle(IFormatProvider)"/>
    float IConvertible.ToSingle(IFormatProvider provider) => Convert.ToSingle(this.Value);

    /// <inheritdoc cref="IConvertible.ToDouble(IFormatProvider)"/>
    double IConvertible.ToDouble(IFormatProvider provider) => Convert.ToDouble(this.Value);

    /// <inheritdoc cref="IConvertible.ToDecimal(IFormatProvider)"/>
    decimal IConvertible.ToDecimal(IFormatProvider provider) => Convert.ToDecimal(this.Value);

    /// <inheritdoc cref="IConvertible.ToString(IFormatProvider)"/>
    string IConvertible.ToString(IFormatProvider provider) => Value.ToString(provider);

    /// <inheritdoc cref="IConvertible.ToDateTime(IFormatProvider)"/>
    DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw new InvalidCastException("Cannot cast from Atomic<T> to DateTime.");

    /// <inheritdoc cref="IConvertible.ToType(Type, IFormatProvider)"/>
    object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Convert.ChangeType(this, conversionType, provider);

    #endregion |IConvertible Members|

    #region Auto-Generated Items

    /// <inheritdoc cref="object.GetHashCode"/>
    public override int GetHashCode() => (int)(dynamic)this.Value ^ (int)((dynamic)this.Value >> 32);

    private string GetDebuggerDisplay() => ToString();

    #endregion Auto-Generated Items
}
