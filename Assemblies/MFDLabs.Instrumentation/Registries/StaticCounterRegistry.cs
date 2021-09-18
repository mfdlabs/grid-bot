using System;

namespace MFDLabs.Instrumentation
{
    public static class StaticCounterRegistry
    {
        public static ICounterRegistry Instance { get; } = new CounterRegistry();

        public static Action<Exception> ExceptionHandler { get; set; }

        static StaticCounterRegistry()
        {
            new CounterReporter(Instance, new Action<Exception>(HandleException)).Start();
        }

        private static void HandleException(Exception ex)
        {
            try
            {
                ExceptionHandler?.Invoke(ex);
            }
            catch
            {
            }
        }
    }
}
