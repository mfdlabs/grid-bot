using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace MFDLabs.Threading
{
    /// <summary>
    /// Represents an atomically accessible number.
    /// </summary>
    [Serializable]
    [ComVisible(true)]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct Atomic<T> : 
        IComparable, 
        IFormattable, 
        IConvertible, 
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

        public T Value
        {
            get
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

        public Atomic() => this.Value = (T)(dynamic)0;
        public Atomic(T value) => this.Value = value;
        public Atomic(Atomic<T> other) => this.Value = other.Value;

        #endregion |Constructors|

        #region |Operators|

        #region |Equality|

        public static bool operator ==(Atomic<T> self, Atomic<T> obj) => EqualityComparer<T>.Default.Equals(self.Value, obj.Value);
        public static bool operator !=(Atomic<T> self, Atomic<T> obj) => !EqualityComparer<T>.Default.Equals(self.Value, obj.Value);
        public static bool operator ==(Atomic<T> self, T obj) => EqualityComparer<T>.Default.Equals(self.Value, obj);
        public static bool operator !=(Atomic<T> self, T obj) => !EqualityComparer<T>.Default.Equals(self.Value, obj);

        #endregion |Equality|

        #region |Less/Greater Than|

        public static bool operator <(Atomic<T> left, Atomic<T> right) => left.Value.CompareTo(right.Value) < 0;
        public static bool operator >(Atomic<T> left, Atomic<T> right) => left.Value.CompareTo(right.Value) > 0;
        public static bool operator <(Atomic<T> left, T right) => left.Value.CompareTo(right) < 0;
        public static bool operator >(Atomic<T> left, T right) => left.Value.CompareTo(right) > 0;

        #endregion |Less/Greater Than|

        #endregion |Operators|

        #region |Implicit Cast Operators|

        public static implicit operator T(Atomic<T> self) => self.Value;
        public static implicit operator Atomic<T>(T v) => new(v);

        #endregion |Implicit Cast Operators|

        #region |Compare And Swap|

        public T CompareAndSwap(T value, T comparand) => (T)Interlocked.CompareExchange(ref this._value, value, comparand);

        #endregion |Compare And Swap|

        #region |Swap|

        public T Swap(T value) => (T)Interlocked.Exchange(ref this._value, value);

        #endregion |Swap|

        #region |Arithmetic Operators|

        public static Atomic<T> operator ++(Atomic<T> self)
        {
            dynamic v = self.Value;

            return self.Value = ++v;
        }
        public static Atomic<T> operator --(Atomic<T> self)
        {
            dynamic v = self.Value;

            return self.Value = --v;
        }

        #endregion |Arithmetic Operators|

        #region |IComparable Members|

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

        public int CompareTo(Atomic<T> obj)
        {
            if (this < obj) return -1;
            if (this > obj) return 1;
            return 0;
        }

        #endregion |IComparable Members|

        #region |IEquatable Members|

        public override bool Equals(object obj) => obj is Atomic<T> atomic && this == atomic;
        public bool Equals(Atomic<T> atomic) => this == atomic;

        #endregion |IEquatable Members|

        #region |IFormattable Members|

        public override string ToString() => Value.ToString();
        public string ToString(IFormatProvider provider) => Value.ToString(provider);
        public string ToString(string format, IFormatProvider provider) => Value.ToString(format, provider);

        #endregion |IFormattable Members|

        #region |IConvertible Members|

        public TypeCode GetTypeCode() => this.Value.GetTypeCode();
        bool IConvertible.ToBoolean(IFormatProvider provider) => Convert.ToBoolean(this.Value);
        char IConvertible.ToChar(IFormatProvider provider) => Convert.ToChar(this.Value);
        sbyte IConvertible.ToSByte(IFormatProvider provider) => Convert.ToSByte(this.Value);
        byte IConvertible.ToByte(IFormatProvider provider) => Convert.ToByte(this.Value);
        short IConvertible.ToInt16(IFormatProvider provider) => Convert.ToInt16(this.Value);
        ushort IConvertible.ToUInt16(IFormatProvider provider) => Convert.ToUInt16(this.Value);
        int IConvertible.ToInt32(IFormatProvider provider) => Convert.ToInt32(this.Value);
        uint IConvertible.ToUInt32(IFormatProvider provider) => Convert.ToUInt32(this.Value);
        long IConvertible.ToInt64(IFormatProvider provider) => Convert.ToInt64(this.Value);
        ulong IConvertible.ToUInt64(IFormatProvider provider) => Convert.ToUInt64(this.Value);
        float IConvertible.ToSingle(IFormatProvider provider) => Convert.ToSingle(this.Value);
        double IConvertible.ToDouble(IFormatProvider provider) => Convert.ToDouble(this.Value);
        decimal IConvertible.ToDecimal(IFormatProvider provider) => Convert.ToDecimal(this.Value);
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw new InvalidCastException("Cannot cast from Atomic<T> to DateTime.");
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Convert.ChangeType(this, conversionType, provider);

        #endregion |IConvertible Members|

        #region Auto-Generated Items

        public override int GetHashCode() => (int)(dynamic)this.Value ^ (int)((dynamic)this.Value >> 32);
        private string GetDebuggerDisplay() => ToString();

        #endregion Auto-Generated Items
    }
}