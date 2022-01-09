namespace MFDLabs.Grid.Bot
{
    public static class Program
    {
        private static string GetBadConfigurationError()
            => $"Could not locate the application configuration at the files '{System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}' " +
            $"or '{System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "app.config")}' with your " +
            "distribution, please install the app correctly and try again.";

        public static void Main()
        {
            if (!System.IO.File.Exists(System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
                if (System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
                        "app.config")))
                    System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile =
                        System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "app.config");
                else
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                    System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.Program_Main_BadConfiguration, GetBadConfigurationError());
                    System.Console.ResetColor();
                    return;
                }

            System.AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is System.IO.FileNotFoundException or System.TypeLoadException)
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                    System.Console.WriteLine(
                        global::MFDLabs.Grid.Bot.Properties.Resources.Program_Main_UnhandledException,
                        (e.ExceptionObject as System.Exception)?.Message);
                    System.Console.ResetColor();
                    System.Environment.Exit(1);
                    return;
                }

                System.Console.ForegroundColor = System.ConsoleColor.Red;
#if DEBUG
                System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.Program_Main_GlobalException, e.ExceptionObject);
#else
                System.Console.WriteLine(global::MFDLabs.Grid.Bot.Properties.Resources.Program_Main_GlobalException, (e.ExceptionObject as System.Exception).Message);
#endif
                System.Console.ResetColor();

                MFDLabs.Grid.Bot.CrashHandler.Upload(e.ExceptionObject as System.Exception);
                
                if (e.ExceptionObject is System.InvalidOperationException) 
                    System.Environment.Exit(1);
            };

            MFDLabs.Grid.Bot.Runner.Invoke();
        }
    }
}
