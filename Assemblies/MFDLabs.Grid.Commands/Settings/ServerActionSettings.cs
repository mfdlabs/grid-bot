namespace MFDLabs.Grid.Commands
{
    public class ServerActionSettings
    {
        public ServerActionType Action { get; }

        public ServerActionReason Reason { get; }

        public string VerboseReason { get; }

        public ServerActionSettings(ServerActionType serverActionType, ServerActionReason reason, string verboseReason)
        {
            Action = serverActionType;
            Reason = reason;
            VerboseReason = verboseReason;
        }
    }
}
