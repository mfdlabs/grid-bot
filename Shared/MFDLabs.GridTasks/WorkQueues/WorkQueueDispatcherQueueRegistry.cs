using MFDLabs.Concurrency;
using Microsoft.Ccr.Core;


namespace MFDLabs.Grid.Bot.Tasks.WorkQueues
{
    internal static class WorkQueueDispatcherQueueRegistry
    {
        public static readonly DispatcherQueue RenderQueue = new PatchedDispatcherQueue("Render Queue", new(0, "Render Queue Dispatcher"));
    }
}
