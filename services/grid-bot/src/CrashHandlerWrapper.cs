namespace Grid.Bot
{
    internal static class CrashHandlerWrapper
    {
        public static void Upload(System.Exception ex, bool overrideSystemWhenExitingCrashHandler = false) => Grid.Bot.Utility.CrashHandler.Upload(ex, overrideSystemWhenExitingCrashHandler);
    }
}
