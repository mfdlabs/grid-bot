namespace MFDLabs.Grid.Bot
{
    internal static class CrashHandlerWrapper
    {
        public static void Upload(System.Exception ex, bool overrideSystemWhenExitingCrashHandler = false) => MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, overrideSystemWhenExitingCrashHandler);
    }
}
