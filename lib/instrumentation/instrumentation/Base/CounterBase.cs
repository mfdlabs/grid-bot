namespace Instrumentation
{
    public abstract class CounterBase
    {
        public CounterKey Key { get; }

        protected CounterBase(string category, string name, string instance) 
            => Key = new CounterKey(category, name, instance);

        public double GetLastFlushValue() => LastFlushValue;
        internal abstract double Flush();
        internal abstract double Get();

        internal double LastFlushValue;
    }
}
