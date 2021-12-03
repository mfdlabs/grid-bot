using System;
using System.IO;

namespace MFDLabs.Grid.Bot
{
    public sealed class Program
    {
        private static string GetBadConfigurationError()
            => $"Could not locate the application configuration at the files '{AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}' " +
            $"or '{Directory.GetCurrentDirectory()}\\app.config' with your distribution, please install the app correctly and try again.";

        public static void Main()
        {
            if (!File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
                if (File.Exists($"{Directory.GetCurrentDirectory()}\\app.config"))
                    AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = $"{Directory.GetCurrentDirectory()}\\app.config";
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[URGENT]: {GetBadConfigurationError()}");
                    Console.ResetColor();
                    return;
                }

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is FileNotFoundException || e.ExceptionObject is TypeLoadException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"There was an error loading a type or dependency, please review the following error: {(e.ExceptionObject as Exception).Message}");
                    Console.ResetColor();
                    Environment.Exit(1);
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Red;
#if DEBUG
                Console.WriteLine($"[URGENT]: Unhandled global exception occurred: {e.ExceptionObject}");
#else
                Console.WriteLine($"[URGENT]: Unhandled global exception occurred: {(e.ExceptionObject as Exception).Message}");
#endif
                Console.ResetColor();
            };

            Runner.Invoke();
        }
    }
}
