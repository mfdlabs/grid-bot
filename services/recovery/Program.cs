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

    /// <summary>
    /// Main method.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public static void Main(string[] args)
    {
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
