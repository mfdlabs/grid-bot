namespace MFDLabs.Grid.Bot.Utility
{
    public static class CrashHandler
    {
        public static void Upload(System.Exception ex, bool overrideSystemWhenExitingCrashHandler = false)
        {
            if (string.IsNullOrEmpty(global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerURL) ||
                string.IsNullOrEmpty(global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerAccessToken))
                return;
            if (ex == null)
                return;

            var traceBack = global::MFDLabs.ErrorHandling.Extensions.ExceptionExtensions.ToDetailedString(
                new global::System.Exception("Crash Handler call traceback.", ex)
            );

            global::System.Threading.ThreadPool.QueueUserWorkItem(s =>
            {
                try
                {
                    System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.CrashHandler_UploadRunning);
                    System.Console.WriteLine(traceBack);
                    var bckTraceCreds = new MFDLabs.Backtrace.Model.BacktraceCredentials(
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerURL,
                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerAccessToken
                    );
                    var crashUploaderClient = new MFDLabs.Backtrace.BacktraceClient(bckTraceCreds);
                    crashUploaderClient.Send(new MFDLabs.Backtrace.Model.BacktraceReport(ex));
                    System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.CrashHandler_Upload_Success);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.CrashHandlerExitWhenDone && !overrideSystemWhenExitingCrashHandler)
                        System.Environment.Exit(1);
                }
                catch (global::System.Exception e)
                {
                    System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.CrashHandler_Upload_Failure, e.ToString());
                }
            });
        }
    }
}
