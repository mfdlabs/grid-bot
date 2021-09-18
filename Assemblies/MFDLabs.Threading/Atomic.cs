using System.Diagnostics;
using System.Threading;


namespace MFDLabs.Threading
{
    public sealed class Atomic
    {
        private long v;

        public Atomic(long value)
        {
            v = value;
            Debug.Assert((this.v & (sizeof(long) - 1)) == 0);
        }

        public long CompareAndSwap(long value, long comparand)
        {
            return Interlocked.CompareExchange(ref v, value, comparand);
        }

        public static Atomic operator ++(Atomic atomic)
        {
            Interlocked.Increment(ref atomic.v);
            return atomic;
        }

        public static Atomic operator --(Atomic atomic)
        {
            Interlocked.Decrement(ref atomic.v);
            return atomic;
        }

        public long Swap(long value)
        {
            return Interlocked.Exchange(ref v, value);
        }
    }
}
