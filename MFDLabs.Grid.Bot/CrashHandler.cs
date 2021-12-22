namespace MFDLabs.Grid.Bot
{
    internal static class CrashHandler
    {
        internal static void Upload(System.Exception ex)
        {
            if (string.IsNullOrEmpty(global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerURL) ||
                string.IsNullOrEmpty(global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerAccessToken))
                return;
            try
            {
                System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.CrashHandler_UploadRunning);
                var bckTraceCreds = new Backtrace.Model.BacktraceCredentials(
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerURL,
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerAccessToken
                );
                var crashUploaderClient = new Backtrace.BacktraceClient(bckTraceCreds);
                crashUploaderClient.Send(new Backtrace.Model.BacktraceReport(ex));
                System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.CrashHandler_Upload_Success);
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerExitWhenDone)
                    System.Environment.Exit(1);
            }
            catch
            {
                System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.CrashHandler_Upload_Failure);
            }
        }
    }
}
