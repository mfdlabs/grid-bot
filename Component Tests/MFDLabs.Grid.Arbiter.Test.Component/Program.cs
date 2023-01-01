namespace MFDLabs.Grid.Arbiter.Test.Component;

using System.ServiceModel;

using Utility;
using Logging;
using Instrumentation;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var defaultHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                SendTimeout = TimeSpan.MaxValue
            };

            EventLogLogger.Singleton.Warning("BLAH");

            GridServerArbiter.SetDefaultHttpBinding(defaultHttpBinding);
            GridServerArbiter.SetCounterRegistry(StaticCounterRegistry.Instance);

            GridServerArbiter.Singleton.KillAllInstances();

            //var inst = GridServerArbiter.Singleton.GetOrCreateAvailableLeasedInstance(TimeSpan.FromSeconds(5));

            if (!args.Contains("--skip"))
            {

                Enumerable.Range(0, 10).ToList().ForEach(inst => Task.Run(() => GridServerArbiter.Singleton.ExecuteScript("return 'HEYO!'")));

                Task.Delay(TimeSpan.FromSeconds(10)).Wait();

                GridServerArbiter.Singleton.KillAllInstances();
            }
            else
            {
                Task.Delay(TimeSpan.FromSeconds(10)).Wait();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Logger.Singleton.Error(ex);
        }
    }
}
