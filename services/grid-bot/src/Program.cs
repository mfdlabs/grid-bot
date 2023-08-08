namespace MFDLabs.Grid.Bot
{
    public static class Program
    {
        public static bool AssemblyIsLoaded(string name)
        {
            try
            {
                return global::System.AppDomain.CurrentDomain.Load(name) != null;
            }
            // We assume this means that it's already loaded into another evidence
            catch (global::System.IO.FileLoadException) { return true; }
            catch (global::System.Exception) { return false; }
        }

        private static string GetBadConfigurationError()
            => $"Could not locate the application configuration at the files '{(global::System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile)}' " +
            $"or '{(global::System.IO.Path.Combine(global::System.IO.Directory.GetCurrentDirectory(), "app.config"))}' with your " +
            "distribution, please install the app correctly and try again.";

        public static void Main(string[] args)
        {
            if (!global::System.IO.File.Exists(
                global::System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            ))
                if (global::System.IO.File.Exists(
                    global::System.IO.Path.Combine(
                        global::System.IO.Directory.GetCurrentDirectory(),
                        "app.config"
                    )
                ))
                    global::System.AppDomain.CurrentDomain.SetupInformation.ConfigurationFile = global::System.IO.Path.Combine(
                        global::System.IO.Directory.GetCurrentDirectory(),
                        "app.config"
                    );
                else
                {
                    global::System.Console.ForegroundColor = global::System.ConsoleColor.Red;
                    global::System.Console.WriteLine("[URGENT]: {0}", GetBadConfigurationError());
                    global::System.Console.ResetColor();

                    if (AssemblyIsLoaded("Backtrace") && AssemblyIsLoaded("MFDLabs.GridSettings") && AssemblyIsLoaded("MFDLabs.GridUtility"))
                        global::MFDLabs.Grid.Bot.CrashHandlerWrapper.Upload(new global::System.ApplicationException(GetBadConfigurationError()), true);

                    return;
                }

            System.AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is global::System.IO.FileNotFoundException or global::System.TypeLoadException)
                {
                    global::System.Console.ForegroundColor = global::System.ConsoleColor.Yellow;
                    global::System.Console.WriteLine(
                        "There was an error loading a type or dependency, please review the following error: {0}",
                        (e.ExceptionObject as global::System.Exception)?.Message
                    );
                    global::System.Console.ResetColor();
                    global::System.Console.WriteLine("Press any key to exit...");
                    global::System.Console.ReadKey(true);
                    global::System.Environment.Exit(1);
                    return;
                }

                global::System.Console.ForegroundColor = global::System.ConsoleColor.Red;
                global::System.Console.WriteLine("[URGENT]: Unhandled global exception occurred: {0}", e.ExceptionObject);
                global::System.Console.ResetColor();

                if (AssemblyIsLoaded("Backtrace") && AssemblyIsLoaded("MFDLabs.GridSettings") && AssemblyIsLoaded("MFDLabs.GridUtility"))
                    global::MFDLabs.Grid.Bot.CrashHandlerWrapper.Upload(e.ExceptionObject as global::System.Exception, true);

                global::MFDLabs.Grid.Bot.Runner.OnGlobalException(e.ExceptionObject as global::System.Exception);

                if (e.ExceptionObject is global::System.AggregateException aggregate)
                {
                    if (global::System.Linq.Enumerable.Any(
                        global::System.Linq.Enumerable.Where(
                            aggregate.InnerExceptions,
                            x => x is global::System.InvalidOperationException
                        )
                    ))
                    {
                        global::System.Console.WriteLine("Press any key to exit...");
                        global::System.Console.ReadKey(true);
                        global::System.Environment.Exit(1);
                    }
                }
            };

            global::MFDLabs.Grid.Bot.Runner.Invoke(args);
        }
    }
}
