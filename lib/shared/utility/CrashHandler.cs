﻿namespace Grid.Bot.Utility
{
    public static class CrashHandler
    {
        public static void Upload(System.Exception ex, bool overrideSystemWhenExitingCrashHandler = false)
        {
            if (string.IsNullOrEmpty(global::Grid.Bot.Properties.Settings.Default.CrashHandlerURL) ||
                string.IsNullOrEmpty(global::Grid.Bot.Properties.Settings.Default.CrashHandlerAccessToken))
                return;
            if (ex == null)
                return;

            var traceBack = $"Error: {ex}\nTrace: {(global::System.Environment.StackTrace)}"; 

            global::System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    System.Console.WriteLine(global::Grid.Bot.Properties.Resources.CrashHandler_UploadRunning);
                    System.Console.WriteLine(traceBack);
                    var bckTraceCreds = new Backtrace.Model.BacktraceCredentials(
                        global::Grid.Bot.Properties.Settings.Default.CrashHandlerURL,
                        global::Grid.Bot.Properties.Settings.Default.CrashHandlerAccessToken
                    );
                    var crashUploaderClient = new Backtrace.BacktraceClient(bckTraceCreds);
                    crashUploaderClient.Send(new Backtrace.Model.BacktraceReport(ex));
                    System.Console.WriteLine(global::Grid.Bot.Properties.Resources.CrashHandler_Upload_Success);
                    if (global::Grid.Bot.Properties.Settings.Default.CrashHandlerExitWhenDone && !overrideSystemWhenExitingCrashHandler)
                        System.Environment.Exit(1);
                }
                catch (global::System.Exception e)
                {
                    System.Console.WriteLine(global::Grid.Bot.Properties.Resources.CrashHandler_Upload_Failure, e.ToString());
                }
            });
        }
    }
}
