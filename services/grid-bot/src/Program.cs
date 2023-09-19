namespace Grid.Bot;

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Main entry point.
/// </summary>
public static class Program
{
    private static bool AssemblyIsLoaded(string name)
    {
        try
        {
            return AppDomain.CurrentDomain.Load(name) != null;
        }
        // We assume this means that it's already loaded into another evidence
        catch (FileLoadException) { return true; }
        catch (Exception) { return false; }
    }

    private static string GetBadConfigurationError()
        => $"Could not locate the application configuration at the files '{(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)}' " +
        $"or '{(Path.Combine(Directory.GetCurrentDirectory(), "app.config"))}' with your " +
        "distribution, please install the app correctly and try again.";

    /// <summary>
    /// Main method.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public static void Main(string[] args)
    {
        if (!File.Exists(
            AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
        ))
            if (File.Exists(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "app.config"
                )
            ))
                AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "app.config"
                );
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[URGENT]: {0}", GetBadConfigurationError());
                Console.ResetColor();

                if (AssemblyIsLoaded("Backtrace") && AssemblyIsLoaded("Shared.Settings") && AssemblyIsLoaded("Shared.Utility"))
                    CrashHandlerWrapper.Upload(new ApplicationException(GetBadConfigurationError()));

                return;
            }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is FileNotFoundException or TypeLoadException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    "There was an error loading a type or dependency, please review the following error: {0}",
                    (e.ExceptionObject as Exception)?.Message
                );
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(1);
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[URGENT]: Unhandled global exception occurred: {0}", e.ExceptionObject);
            Console.ResetColor();

            if (AssemblyIsLoaded("Backtrace") && AssemblyIsLoaded("Shared.Settings") && AssemblyIsLoaded("Shared.Utility"))
                CrashHandlerWrapper.Upload(e.ExceptionObject as Exception);

            Runner.OnGlobalException();

            if (e.ExceptionObject is AggregateException aggregate)
            {
                if (aggregate.InnerExceptions.Any(x => x is InvalidOperationException))
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey(true);
                    Environment.Exit(1);
                }
            }
        };

        Runner.Invoke(args);
    }
}
