using MFDLabs.Concurrency;
using Microsoft.Ccr.Core;


namespace MFDLabs.Grid.Bot.Tasks.WorkQueues
{
    internal static class WorkQueueDispatcherQueueRegistry
    {
        public static readonly DispatcherQueue RenderQueue = new PatchedDispatcherQueue("Render Queue", new(0, "Render Queue Dispatcher"));
        public static readonly DispatcherQueue ScriptExecutionQueue = new PatchedDispatcherQueue("Script Execution Queue", new(0, "Script Execution Queue Dispatcher"));
        public static readonly DispatcherQueue GridServerScreenshotQueue = new PatchedDispatcherQueue("Grid Server Screenshot Queue", new(0, "Grid Server Screenshot Queue Dispatcher"));
    }
}
