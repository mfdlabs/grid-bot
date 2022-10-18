using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable NonReadonlyMemberInGetHashCode

/** TODO: Read lock when reading value? **/

namespace MFDLabs.Threading
{
    /// <summary>
    /// Represents an atomically written 64-bit singed integer.
    /// </summary>
    [Serializable]
    [ComVisible(true)]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public struct Atomic : 
        IComparable, 
        IFormattable, 
        IConvertible, 
        IComparable<Atomic>, 
        IEquatable<Atomic>
    {
        #region |Private Members|

        private long _value;

        #endregion |Private Members|

        #region |Constructors|

        public Atomic() => _value = 0;
        public Atomic(long value) => this._value = value;
        public Atomic(ulong value) => this._value = (long)value;
        public Atomic(int value) => this._value = value;
        public Atomic(uint value) => this._value = value;
        public Atomic(short value) => this._value = value;
        public Atomic(ushort value) => this._value = value;
        public Atomic(char value) => this._value = value;
        public Atomic(byte value) => this._value = value;
        public Atomic(sbyte value) => this._value = value;
        public Atomic(float value) => this._value = (long)value;
        public Atomic(double value) => this._value = (long)value;
        public Atomic(decimal value) => this._value = (long)value;
        public Atomic(Atomic other) => _value = other._value;

        #endregion |Constructors|

        #region |Operators|

        #region |Equality|

        public static bool operator ==(Atomic self, Atomic obj) => self._value == obj._value;
        public static bool operator !=(Atomic self, Atomic obj) => self._value != obj._value;
        public static bool operator ==(Atomic self, ulong obj) => self._value == (long)obj;
        public static bool operator !=(Atomic self, ulong obj) => self._value != (long)obj;
        public static bool operator ==(Atomic self, long obj) => self._value == obj;
        public static bool operator !=(Atomic self, long obj) => self._value != obj;
        public static bool operator ==(Atomic self, int obj) => self._value == obj;
        public static bool operator !=(Atomic self, int obj) => self._value != obj;
        public static bool operator ==(Atomic self, uint obj) => self._value == obj;
        public static bool operator !=(Atomic self, uint obj) => self._value != obj;
        public static bool operator ==(Atomic self, short obj) => self._value == obj;
        public static bool operator !=(Atomic self, short obj) => self._value != obj;
        public static bool operator ==(Atomic self, ushort obj) => self._value == obj;
        public static bool operator !=(Atomic self, ushort obj) => self._value != obj;
        public static bool operator ==(Atomic self, char obj) => self._value == obj;
        public static bool operator !=(Atomic self, char obj) => self._value != obj;
        public static bool operator ==(Atomic self, byte obj) => self._value == obj;
        public static bool operator !=(Atomic self, byte obj) => self._value != obj;
        public static bool operator ==(Atomic self, sbyte obj) => self._value == obj;
        public static bool operator !=(Atomic self, sbyte obj) => self._value != obj;
        public static bool operator ==(Atomic self, float obj) => self._value == obj;
        public static bool operator !=(Atomic self, float obj) => self._value != obj;
        public static bool operator ==(Atomic self, double obj) => self._value == obj;
        public static bool operator !=(Atomic self, double obj) => self._value != obj;
        public static bool operator ==(Atomic self, decimal obj) => self._value == obj;
        public static bool operator !=(Atomic self, decimal obj) => self._value != obj;

        #endregion |Equality|

        #region |Less/Greater Than|

        public static bool operator <(Atomic left, Atomic right) => left._value < right._value;
        public static bool operator >(Atomic left, Atomic right) => left._value > right._value;
        public static bool operator <(Atomic left, ulong right) => left._value < (long)right;
        public static bool operator >(Atomic left, ulong right) => left._value > (long)right;
        public static bool operator <(Atomic left, long right) => left._value < right;
        public static bool operator >(Atomic left, long right) => left._value > right;
        public static bool operator <(Atomic left, uint right) => left._value < right;
        public static bool operator >(Atomic left, uint right) => left._value > right;
        public static bool operator <(Atomic left, int right) => left._value < right;
        public static bool operator >(Atomic left, int right) => left._value > right;
        public static bool operator <(Atomic left, ushort right) => left._value < right;
        public static bool operator >(Atomic left, ushort right) => left._value > right;
        public static bool operator <(Atomic left, short right) => left._value < right;
        public static bool operator >(Atomic left, short right) => left._value > right;
        public static bool operator <(Atomic left, char right) => left._value < right;
        public static bool operator >(Atomic left, char right) => left._value > right;
        public static bool operator <(Atomic left, byte right) => left._value < right;
        public static bool operator >(Atomic left, byte right) => left._value > right;
        public static bool operator <(Atomic left, sbyte right) => left._value < right;
        public static bool operator >(Atomic left, sbyte right) => left._value > right;
        public static bool operator <(Atomic left, float right) => left._value < right;
        public static bool operator >(Atomic left, float right) => left._value > right;
        public static bool operator <(Atomic left, double right) => left._value < right;
        public static bool operator >(Atomic left, double right) => left._value > right;
        public static bool operator <(Atomic left, decimal right) => left._value < right;
        public static bool operator >(Atomic left, decimal right) => left._value > right;

        #endregion |Less/Greater Than|

        #region |Bit Shift|

        public static Atomic operator <<(Atomic left, Atomic right) => left._value << (int)right._value;
        public static Atomic operator >>(Atomic left, Atomic right) => left._value >> (int)right._value;
        public static Atomic operator <<(Atomic left, ulong right) => left._value << (int)right;
        public static Atomic operator >>(Atomic left, ulong right) => left._value >> (int)right;
        public static Atomic operator <<(Atomic left, long right) => left._value << (int)right;
        public static Atomic operator >>(Atomic left, long right) => left._value >> (int)right;
        public static Atomic operator <<(Atomic left, uint right) => left._value << (int)right;
        public static Atomic operator >>(Atomic left, uint right) => left._value >> (int)right;
        public static Atomic operator <<(Atomic left, int right) => left._value << right;
        public static Atomic operator >>(Atomic left, int right) => left._value >> right;
        public static Atomic operator <<(Atomic left, ushort right) => left._value << right;
        public static Atomic operator >>(Atomic left, ushort right) => left._value >> right;
        public static Atomic operator <<(Atomic left, short right) => left._value << right;
        public static Atomic operator >>(Atomic left, short right) => left._value >> right;
        public static Atomic operator <<(Atomic left, char right) => left._value << right;
        public static Atomic operator >>(Atomic left, char right) => left._value >> right;
        public static Atomic operator <<(Atomic left, byte right) => left._value << right;
        public static Atomic operator >>(Atomic left, byte right) => left._value >> right;
        public static Atomic operator <<(Atomic left, sbyte right) => left._value << right;
        public static Atomic operator >>(Atomic left, sbyte right) => left._value >> right;
        public static Atomic operator <<(Atomic left, float right) => left._value << (int)right;
        public static Atomic operator >>(Atomic left, float right) => left._value >> (int)right;
        public static Atomic operator <<(Atomic left, double right) => left._value << (int)right;
        public static Atomic operator >>(Atomic left, double right) => left._value >> (int)right;
        public static Atomic operator <<(Atomic left, decimal right) => left._value << (int)right;
        public static Atomic operator >>(Atomic left, decimal right) => left._value >> (int)right;

        #endregion |Bit Shift|

        #endregion |Operators|

        #region |Implicit Cast Operators|

        public static implicit operator long(Atomic self) => self._value;
        public static implicit operator ulong(Atomic self) => (ulong)self._value;
        public static implicit operator int(Atomic self) => (int)self._value;
        public static implicit operator uint(Atomic self) => (uint)self._value;
        public static implicit operator short(Atomic self) => (short)self._value;
        public static implicit operator ushort(Atomic self) => (ushort)self._value;
        public static implicit operator char(Atomic self) => (char)self._value;
        public static implicit operator byte(Atomic self) => (byte)self._value;
        public static implicit operator sbyte(Atomic self) => (sbyte)self._value;
        public static implicit operator float(Atomic self) => self._value;
        public static implicit operator double(Atomic self) => self._value;
        public static implicit operator decimal(Atomic self) => self._value;
        public static implicit operator Atomic(long v) => new(v);
        public static implicit operator Atomic(ulong v) => new(v);
        public static implicit operator Atomic(int v) => new(v);
        public static implicit operator Atomic(uint v) => new(v);
        public static implicit operator Atomic(short v) => new(v);
        public static implicit operator Atomic(ushort v) => new(v);
        public static implicit operator Atomic(char v) => new(v);
        public static implicit operator Atomic(byte v) => new(v);
        public static implicit operator Atomic(sbyte v) => new(v);
        public static implicit operator Atomic(float v) => new(v);
        public static implicit operator Atomic(double v) => new(v);
        public static implicit operator Atomic(decimal v) => new(v);

        #endregion |Implicit Cast Operators|

        #region |Compare And Swap|

        public long CompareAndSwap(long value, long comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(ulong value, ulong comparand) => Interlocked.CompareExchange(ref this._value, (long)value, (long)comparand);
        public long CompareAndSwap(int value, int comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(uint value, uint comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(short value, short comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(ushort value, ushort comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(char value, char comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(byte value, byte comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(sbyte value, sbyte comparand) => Interlocked.CompareExchange(ref this._value, value, comparand);
        public long CompareAndSwap(float value, float comparand) => Interlocked.CompareExchange(ref this._value, (long)value, (long)comparand);
        public long CompareAndSwap(double value, double comparand) => Interlocked.CompareExchange(ref this._value, (long)value, (long)comparand);
        public long CompareAndSwap(decimal value, decimal comparand) => Interlocked.CompareExchange(ref this._value, (long)value, (long)comparand);
        
        #endregion |Compare And Swap|

        #region |Swap|

        public long Swap(long value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(ulong value) => Interlocked.Exchange(ref this._value, (long)value);
        public long Swap(int value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(uint value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(short value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(ushort value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(char value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(byte value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(sbyte value) => Interlocked.Exchange(ref this._value, value);
        public long Swap(float value) => Interlocked.Exchange(ref this._value, (long)value);
        public long Swap(double value) => Interlocked.Exchange(ref this._value, (long)value);
        public long Swap(decimal value) => Interlocked.Exchange(ref this._value, (long)value);
        
        #endregion |Swap|

        #region |Arithmetic Operators|

        public static Atomic operator ++(Atomic self) => Interlocked.Increment(ref self._value);
        public static Atomic operator --(Atomic self) => Interlocked.Decrement(ref self._value);
        
        #endregion |Arithmetic Operators|

        #region |IComparable Members|

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is Atomic at)
            {
                if (this < at) return -1;
                if (this > at) return 1;
                return 0;
            }

            throw new ArgumentException("obj must be of type Atomic.");
        }

        public int CompareTo(Atomic obj)
        {
            if (this < obj) return -1;
            if (this > obj) return 1;
            return 0;
        }

        #endregion |IComparable Members|

        #region |IEquatable Members|

        public override bool Equals(object obj) => obj is Atomic atomic && _value == atomic._value;
        public bool Equals(Atomic atomic) => _value == atomic._value;

        #endregion |IEquatable Members|

        #region |IFormattable Members|

        public override string ToString() => _value.ToString();
        public string ToString(IFormatProvider provider) => _value.ToString(provider);
        public string ToString(string format) => _value.ToString(format);
        public string ToString(string format, IFormatProvider provider) => _value.ToString(format, provider);

        #endregion |IFormattable Members|

        #region |IConvertible Members|

        public TypeCode GetTypeCode() => TypeCode.Int64;
        bool IConvertible.ToBoolean(IFormatProvider provider) => Convert.ToBoolean(this._value);
        char IConvertible.ToChar(IFormatProvider provider) => Convert.ToChar(this._value);
        sbyte IConvertible.ToSByte(IFormatProvider provider) => Convert.ToSByte(this._value);
        byte IConvertible.ToByte(IFormatProvider provider) => Convert.ToByte(this._value);
        short IConvertible.ToInt16(IFormatProvider provider) => Convert.ToInt16(this._value);
        ushort IConvertible.ToUInt16(IFormatProvider provider) => Convert.ToUInt16(this._value);
        int IConvertible.ToInt32(IFormatProvider provider) => Convert.ToInt32(this._value);
        uint IConvertible.ToUInt32(IFormatProvider provider) => Convert.ToUInt32(this._value);
        long IConvertible.ToInt64(IFormatProvider provider) => this._value;
        ulong IConvertible.ToUInt64(IFormatProvider provider) => Convert.ToUInt64(this._value);
        float IConvertible.ToSingle(IFormatProvider provider) => Convert.ToSingle(this._value);
        double IConvertible.ToDouble(IFormatProvider provider) => Convert.ToDouble(this._value);
        decimal IConvertible.ToDecimal(IFormatProvider provider) => Convert.ToDecimal(this._value);
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw new InvalidCastException("Cannot cast from Atomic to DateTime.");
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Convert.ChangeType(this, conversionType, provider);

        #endregion |IConvertible Members|

        #region Auto-Generated Items

        public override int GetHashCode() => (int)this ^ (int)(this >> 32);
        private string GetDebuggerDisplay() => ToString();

        #endregion Auto-Generated Items
    }
}