using Newtonsoft.Json;

namespace MFDLabs.Grid.Commands
{
    public class SetLimitsCommand : GridCommand
    {
        public override string Mode => "SetLimits";
        public override int MessageVersion => 1;
        public SetLimitsSettings Limits { get; }

        public SetLimitsCommand(SetLimitsSettings limitSettings) => Limits = limitSettings;

        public string ToJsonString(bool useStringForNumber)
        {
            if (!useStringForNumber) return JsonConvert.SerializeObject(this);
            return JsonConvert.SerializeObject(new
            {
                Mode,
                MessageVersion,
                Limits = new
                {
                    MaximumCores = Limits.MaximumCores.ToString(),
                    MaximumThreads = Limits.MaximumThreads.ToString(),
                    MaximumMemoryMB = Limits.MaximumMemoryMB.ToString(),
                    SchedulerCpuPeriod = Limits.SchedulerCpuPeriod.ToString()
                }
            }, Formatting.None);
        }
    }
}