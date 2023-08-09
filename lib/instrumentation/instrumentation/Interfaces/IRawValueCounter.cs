namespace MFDLabs.Instrumentation
{
    public interface IRawValueCounter
    {
        void Set(long value);
        void IncrementBy(long value);
        void Increment();
        void Decrement();

        long RawValue { get; }
    }
}
