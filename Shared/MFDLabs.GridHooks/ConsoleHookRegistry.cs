/*
    Name: ConsoleHookRegistry.cs
    Written By: Alex Bkordan
    Description: C# Runtime parser for a console hook registry (hooking onto key presses)
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Reflection.Extensions;

namespace MFDLabs.Grid.Bot.Registries
{
    public static class ConsoleHookRegistry
    {

        private static bool _wasRegistered;

        private static readonly object RegistrationLock = new();

        private static readonly ICollection<IConsoleHook> ConsoleHooks = new List<IConsoleHook>();

        private static string GetHookNamespace() => "MFDLabs.Grid.Bot.Hooks";

        /* TODO: Use reflection to shorten the code in here! */
        public static void Register()
        {
            RegisterInternal();

            Console.TreatControlCAsInput = true;
            Console.CursorVisible = false;

            var thread = new Thread(HookThread)
            {
                Name = "Console Hook",
                IsBackground = true
            };
            thread.Start();
        }

        private static IConsoleHook GetConsoleHook(char key) =>
            (from consoleHook in ConsoleHooks
                where consoleHook.HookKeys.Contains(key)
                select consoleHook).FirstOrDefault();

        private static void RegisterInternal()
        {
            if (_wasRegistered) return;
            
            lock (RegistrationLock)
            {
                ParseAndInsertIntoConsoleHookRegistry();
                _wasRegistered = true;
            }
        }

        private static void ParseAndInsertIntoConsoleHookRegistry()
        {
            Logger.Singleton.LifecycleEvent("Begin attempt to register console hooks via Reflection.");

            try
            {
                var @namespace = GetHookNamespace();

                Logger.Singleton.Info("Got console hook namespace '{0}'.", @namespace);

                var types = Assembly.GetExecutingAssembly().GetTypesInAssemblyNamespace(@namespace);

                if (types.Length == 0)
                {
                    Logger.Singleton.Warning("There were no console hooks found in the namespace '{0}'.", @namespace);
                    return;
                }

                foreach (var type in types)
                {
                    if (type.IsClass)
                    {
                        var consoleHook = Activator.CreateInstance(type);

                        if (consoleHook is IConsoleHook trueConsoleHook)
                        {
                            Logger.Singleton.Info("Parsing console hook '{0}'.", type.FullName);

                            if (trueConsoleHook.HookKeys.Length < 1)
                            {
                                Logger.Singleton.Trace(
                                    "Exception when reading '{0}': Expected the sizeof field 'HookKeys' to be greater than 0, got {1}",
                                    type.FullName,
                                    trueConsoleHook.HookKeys.Length
                                );

                                continue;
                            }

                            ConsoleHooks.Add(trueConsoleHook);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Singleton.Error(ex);
            }
            finally
            {
                Logger.Singleton.Verbose("Successfully initialized the ConsoleHookRegistry.");
            }
        }

        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private static void HookThread()
        {
            while (true)
            {
                var key = Console.ReadKey(true).KeyChar;

                var consoleHook = GetConsoleHook(key);

                if (consoleHook != default)
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        consoleHook.Callback(key);
                    });
            }
        }
    }
}
