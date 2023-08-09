namespace MFDLabs.Instrumentation
{
    public interface IPercentileCounter
    {
        void Sample(double value);
    }
}
