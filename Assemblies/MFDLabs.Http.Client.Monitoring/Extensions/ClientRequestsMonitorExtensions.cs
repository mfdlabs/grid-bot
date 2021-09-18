using System;
using System.Threading.Tasks;

namespace MFDLabs.Http.Client.Monitoring
{
    public static class ClientRequestsMonitorExtensions
    {
        public static Task Monitor(this ClientRequestsMonitor monitor, string methodName, Func<Task> method)
        {
            return monitor.Monitor(methodName, method);
        }

        public static Task<T> Monitor<T>(this ClientRequestsMonitor monitor, string methodName, Func<Task<T>> method)
        {
            return monitor.Monitor(methodName, method);
        }
    }
}
