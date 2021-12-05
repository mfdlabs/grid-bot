using System.Diagnostics;
using System.Threading;

namespace MFDLabs.Threading
{
    /// <summary>
    /// Pulled from Arctic-Assignments
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class Atomic
    {
        long value;

        public Atomic() => value = 0;
        public Atomic(long value) => this.value = value;
        public Atomic(ulong value) => this.value = (long)value;
        public Atomic(int value) => this.value = value;
        public Atomic(uint value) => this.value = value;
        public Atomic(short value) => this.value = value;
        public Atomic(ushort value) => this.value = value;
        public Atomic(char value) => this.value = value;
        public Atomic(byte value) => this.value = value;
        public Atomic(sbyte value) => this.value = value;
        public Atomic(float value) => this.value = (long)value;
        public Atomic(double value) => this.value = (long)value;
        public Atomic(decimal value) => this.value = (long)value;
        public Atomic(Atomic other) => value = other.value;

        public override bool Equals(object obj) => obj is Atomic atomic && value == atomic.value;
        public static bool operator ==(Atomic self, Atomic obj) => self.value == obj?.value;
        public static bool operator !=(Atomic self, Atomic obj) => self.value != obj?.value;
        public static bool operator ==(Atomic self, ulong obj) => self.value == (long)obj;
        public static bool operator !=(Atomic self, ulong obj) => self.value != (long)obj;
        public static bool operator ==(Atomic self, long obj) => self.value == obj;
        public static bool operator !=(Atomic self, long obj) => self.value != obj;
        public static bool operator ==(Atomic self, int obj) => self.value == obj;
        public static bool operator !=(Atomic self, int obj) => self.value != obj;
        public static bool operator ==(Atomic self, uint obj) => self.value == obj;
        public static bool operator !=(Atomic self, uint obj) => self.value != obj;
        public static bool operator ==(Atomic self, short obj) => self.value == obj;
        public static bool operator !=(Atomic self, short obj) => self.value != obj;
        public static bool operator ==(Atomic self, ushort obj) => self.value == obj;
        public static bool operator !=(Atomic self, ushort obj) => self.value != obj;
        public static bool operator ==(Atomic self, char obj) => self.value == obj;
        public static bool operator !=(Atomic self, char obj) => self.value != obj;
        public static bool operator ==(Atomic self, byte obj) => self.value == obj;
        public static bool operator !=(Atomic self, byte obj) => self.value != obj;
        public static bool operator ==(Atomic self, sbyte obj) => self.value == obj;
        public static bool operator !=(Atomic self, sbyte obj) => self.value != obj;
        public static bool operator ==(Atomic self, float obj) => self.value == obj;
        public static bool operator !=(Atomic self, float obj) => self.value != obj;
        public static bool operator ==(Atomic self, double obj) => self.value == obj;
        public static bool operator !=(Atomic self, double obj) => self.value != obj;
        public static bool operator ==(Atomic self, decimal obj) => self.value == obj;
        public static bool operator !=(Atomic self, decimal obj) => self.value != obj;

        public static implicit operator long(Atomic self) => self.value;
        public static implicit operator ulong(Atomic self) => (ulong)self.value;
        public static implicit operator int(Atomic self) => (int)self.value;
        public static implicit operator uint(Atomic self) => (uint)self.value;
        public static implicit operator short(Atomic self) => (short)self.value;
        public static implicit operator ushort(Atomic self) => (ushort)self.value;
        public static implicit operator char(Atomic self) => (char)self.value;
        public static implicit operator byte(Atomic self) => (byte)self.value;
        public static implicit operator sbyte(Atomic self) => (sbyte)self.value;
        public static implicit operator float(Atomic self) => (float)self.value;
        public static implicit operator double(Atomic self) => (double)self.value;
        public static implicit operator decimal(Atomic self) => (decimal)self.value;
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

        public long CompareAndSwap(long value, long comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(ulong value, ulong comparand) => Interlocked.CompareExchange(ref this.value, (long)value, (long)comparand);
        public long CompareAndSwap(int value, int comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(uint value, uint comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(short value, short comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(ushort value, ushort comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(char value, char comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(byte value, byte comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(sbyte value, sbyte comparand) => Interlocked.CompareExchange(ref this.value, value, comparand);
        public long CompareAndSwap(float value, float comparand) => Interlocked.CompareExchange(ref this.value, (long)value, (long)comparand);
        public long CompareAndSwap(double value, double comparand) => Interlocked.CompareExchange(ref this.value, (long)value, (long)comparand);
        public long CompareAndSwap(decimal value, decimal comparand) => Interlocked.CompareExchange(ref this.value, (long)value, (long)comparand);
        public override int GetHashCode() => -1584136870 + value.GetHashCode();

        public static Atomic operator ++(Atomic self) => Interlocked.Increment(ref self.value);
        public static Atomic operator --(Atomic self) => Interlocked.Decrement(ref self.value);

        public long Swap(long value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(ulong value) => Interlocked.Exchange(ref this.value, (long)value);
        public long Swap(int value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(uint value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(short value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(ushort value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(char value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(byte value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(sbyte value) => Interlocked.Exchange(ref this.value, value);
        public long Swap(float value) => Interlocked.Exchange(ref this.value, (long)value);
        public long Swap(double value) => Interlocked.Exchange(ref this.value, (long)value);
        public long Swap(decimal value) => Interlocked.Exchange(ref this.value, (long)value);
        public override string ToString() => value.ToString();
        private string GetDebuggerDisplay() => ToString();
    }
}