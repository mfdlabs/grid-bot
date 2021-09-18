namespace MFDLabs.Instrumentation
{
    public interface IAverageValueCounter
    {
        void Sample(double value);
    }
}
