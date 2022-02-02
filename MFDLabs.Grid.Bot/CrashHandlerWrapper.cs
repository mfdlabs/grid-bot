namespace MFDLabs.Grid.Bot
{
    internal static class CrashHandlerWrapper
    {
        public static void Upload(System.Exception ex) => MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex);
    }
}
