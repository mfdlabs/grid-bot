namespace MFDLabs.Grid.Commands
{
    public class RunMicroProfilerCommand : GridCommand
    {
        public override string Mode => "RunMicroProfiler";

        public override int MessageVersion => 1;

        public RunMicroProfilerSettings Settings { get; }

        public RunMicroProfilerCommand(RunMicroProfilerSettings settings)
        {
            Settings = settings;
        }
    }
}