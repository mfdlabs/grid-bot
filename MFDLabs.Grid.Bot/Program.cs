namespace MFDLabs.Grid.Bot
{
    public static class Program
    {
        public static bool AssemblyIsLoaded(string name)
        {
            try
            {
                return System.AppDomain.CurrentDomain.Load(name) != null;
            }
            catch (System.IO.FileLoadException) { return true; }
            catch (System.Exception) { return false; }
        }

        private static string GetBadConfigurationError()
            => $"Could not locate the application configuration at the files '{System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}' " +
            $"or '{System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "app.config")}' with your " +
            "distribution, please install the app correctly and try again.";

        public static void Main(string[] args)
        {
            if (!System.IO.File.Exists(
                System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            ))
                if (System.IO.File.Exists(
                    System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "app.config"
                    )
                ))
                    System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "app.config"
                    );
                else
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                    System.Console.WriteLine("[URGENT]: {0}", GetBadConfigurationError());
                    System.Console.ResetColor();
                    return;
                }

            System.AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is System.IO.FileNotFoundException or System.TypeLoadException)
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                    System.Console.WriteLine(
                        "There was an error loading a type or dependency, please review the following error: {0}",
                        (e.ExceptionObject as System.Exception)?.Message
                    );
                    System.Console.ResetColor();
                    System.Environment.Exit(1);
                    return;
                }

                System.Console.ForegroundColor = System.ConsoleColor.Red;
#if DEBUG
                System.Console.WriteLine("[URGENT]: Unhandled global exception occurred: {0}", e.ExceptionObject);
#else
                System.Console.WriteLine("[URGENT]: Unhandled global exception occurred: {0}", (e.ExceptionObject as System.Exception).Message);
#endif
                System.Console.ResetColor();

                if (AssemblyIsLoaded("MFDLabs.Backtrace") && AssemblyIsLoaded("MFDLabs.GridSettings"))
                    MFDLabs.Grid.Bot.CrashHandler.Upload(e.ExceptionObject as System.Exception);

                MFDLabs.Grid.Bot.Runner.OnGlobalException(e.ExceptionObject as System.Exception);

                if (e.ExceptionObject is System.InvalidOperationException)
                    System.Environment.Exit(1);
            };

            MFDLabs.Grid.Bot.Runner.Invoke(args);
        }
    }
}
