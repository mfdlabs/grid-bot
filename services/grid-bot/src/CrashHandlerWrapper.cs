namespace Grid.Bot;

internal static class CrashHandlerWrapper
{
    public static void Upload(System.Exception ex)
        => Grid.Bot.Utility.BacktraceUtility.UploadCrashLog(ex);
}
