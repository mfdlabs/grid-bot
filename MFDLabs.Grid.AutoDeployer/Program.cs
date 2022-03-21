using System;
using MFDLabs.Wcf;
using MFDLabs.Grid.AutoDeployer.Service;

namespace MFDLabs.Grid.AutoDeployer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var app = new ServiceHostApp<AutoDeployerService>();
            app.EventLog.Source = "MFDLabs.Grid.AutoDeployer";

            Console.CancelKeyPress += (sender, e) => app.Stop();

            app.HostOpening += AutoDeployerService.Start;
            app.HostClosing += AutoDeployerService.Stop;
            app.Process(args);
        }
    }
}
