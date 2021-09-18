namespace MFDLabs.Grid.Commands
{
    public class SetLimitsSettings
    {
        public double MaximumCores { get; }

        public long MaximumThreads { get; }

        public long MaximumMemoryMB { get; }

        public long SchedulerCpuPeriod { get; }

        public SetLimitsSettings(double maximumCores, long maximumThreads, long maximumMemoryMB, long schedulerCpuPeriod)
        {
            MaximumCores = maximumCores;
            MaximumThreads = maximumThreads;
            MaximumMemoryMB = maximumMemoryMB;
            SchedulerCpuPeriod = schedulerCpuPeriod;
        }
    }
}