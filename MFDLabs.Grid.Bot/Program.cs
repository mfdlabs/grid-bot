namespace MFDLabs.Grid.Bot
{
    public sealed class Program
    {
        private static string GetBadConfigurationError()
            => $"Could not locate the application configuration at the files '{System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}' " +
            $"or '{System.IO.Directory.GetCurrentDirectory()}\\app.config' with your distribution, please install the app correctly and try again.";

        public static void Main()
        {
            if (!System.IO.File.Exists(System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
                if (System.IO.File.Exists($"{System.IO.Directory.GetCurrentDirectory()}\\app.config"))
                    System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = $"{System.IO.Directory.GetCurrentDirectory()}\\app.config";
                else
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Red;
                    System.Console.WriteLine($"[URGENT]: {GetBadConfigurationError()}");
                    System.Console.ResetColor();
                    return;
                }

            System.AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is System.IO.FileNotFoundException || e.ExceptionObject is System.TypeLoadException)
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Yellow;
                    System.Console.WriteLine($"There was an error loading a type or dependency, please review the following error: {(e.ExceptionObject as System.Exception).Message}");
                    System.Console.ResetColor();
                    System.Environment.Exit(1);
                    return;
                }

                System.Console.ForegroundColor = System.ConsoleColor.Red;
#if DEBUG
                System.Console.WriteLine($"[URGENT]: Unhandled global exception occurred: {e.ExceptionObject}");
#else
                System.Console.WriteLine($"[URGENT]: Unhandled global exception occurred: {(e.ExceptionObject as System.Exception).Message}");
#endif
                System.Console.ResetColor();

                CrashHandler.Upload(e.ExceptionObject as System.Exception);
            };

            MFDLabs.Grid.Bot.Runner.Invoke();
        }
    }
}
