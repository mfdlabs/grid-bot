using System.Diagnostics;
using System.Threading;
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace MFDLabs.Threading
{
    /// <summary>
    /// Pulled from Arctic-Assignments
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class Atomic
    {
        private long _value;

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

        public override bool Equals(object obj) => obj is Atomic atomic && _value == atomic._value;
        public static bool operator ==(Atomic self, Atomic obj) => self?._value == obj?._value;
        public static bool operator !=(Atomic self, Atomic obj) => self?._value != obj?._value;
        public static bool operator ==(Atomic self, ulong obj) => self?._value == (long)obj;
        public static bool operator !=(Atomic self, ulong obj) => self?._value != (long)obj;
        public static bool operator ==(Atomic self, long obj) => self?._value == obj;
        public static bool operator !=(Atomic self, long obj) => self?._value != obj;
        public static bool operator ==(Atomic self, int obj) => self?._value == obj;
        public static bool operator !=(Atomic self, int obj) => self?._value != obj;
        public static bool operator ==(Atomic self, uint obj) => self?._value == obj;
        public static bool operator !=(Atomic self, uint obj) => self?._value != obj;
        public static bool operator ==(Atomic self, short obj) => self?._value == obj;
        public static bool operator !=(Atomic self, short obj) => self?._value != obj;
        public static bool operator ==(Atomic self, ushort obj) => self?._value == obj;
        public static bool operator !=(Atomic self, ushort obj) => self?._value != obj;
        public static bool operator ==(Atomic self, char obj) => self?._value == obj;
        public static bool operator !=(Atomic self, char obj) => self?._value != obj;
        public static bool operator ==(Atomic self, byte obj) => self?._value == obj;
        public static bool operator !=(Atomic self, byte obj) => self?._value != obj;
        public static bool operator ==(Atomic self, sbyte obj) => self?._value == obj;
        public static bool operator !=(Atomic self, sbyte obj) => self?._value != obj;
        public static bool operator ==(Atomic self, float obj) => self?._value == obj;
        public static bool operator !=(Atomic self, float obj) => self?._value != obj;
        public static bool operator ==(Atomic self, double obj) => self?._value == obj;
        public static bool operator !=(Atomic self, double obj) => self?._value != obj;
        public static bool operator ==(Atomic self, decimal obj) => self?._value == obj;
        public static bool operator !=(Atomic self, decimal obj) => self?._value != obj;

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
        public static implicit operator Atomic(long v) => new Atomic(v);
        public static implicit operator Atomic(ulong v) => new Atomic(v);
        public static implicit operator Atomic(int v) => new Atomic(v);
        public static implicit operator Atomic(uint v) => new Atomic(v);
        public static implicit operator Atomic(short v) => new Atomic(v);
        public static implicit operator Atomic(ushort v) => new Atomic(v);
        public static implicit operator Atomic(char v) => new Atomic(v);
        public static implicit operator Atomic(byte v) => new Atomic(v);
        public static implicit operator Atomic(sbyte v) => new Atomic(v);
        public static implicit operator Atomic(float v) => new Atomic(v);
        public static implicit operator Atomic(double v) => new Atomic(v);
        public static implicit operator Atomic(decimal v) => new Atomic(v);

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
        public override int GetHashCode() => -1584136870 + _value.GetHashCode();

        public static Atomic operator ++(Atomic self) => Interlocked.Increment(ref self._value);
        public static Atomic operator --(Atomic self) => Interlocked.Decrement(ref self._value);

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
        public override string ToString() => _value.ToString();
        private string GetDebuggerDisplay() => ToString();
    }
}