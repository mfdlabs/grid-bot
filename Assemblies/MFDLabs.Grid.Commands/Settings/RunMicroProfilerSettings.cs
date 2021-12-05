namespace MFDLabs.Grid.Commands
{
    public class RunMicroProfilerSettings
    {
        public double SecondsToRecord { get; }
        public string OutputFileName { get; }

        public RunMicroProfilerSettings(double secondsToRecord, string outputFileName)
        {
            SecondsToRecord = secondsToRecord;
            OutputFileName = outputFileName;
        }
    }
}