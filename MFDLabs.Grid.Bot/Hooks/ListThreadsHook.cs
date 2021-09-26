using System.Diagnostics;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Hooks
{
    internal sealed class ListThreadsHook : IConsoleHook
    {
        public char[] HookKeys => new char[] { 'l', 'L' };

        public void Callback(char key)
        {
            var threads = Process.GetCurrentProcess().Threads;

            SystemLogger.Singleton.Log("Total thread count: {0}", threads.Count);

            foreach (ProcessThread thread in threads)
            {
                SystemLogger.Singleton.Log("Thread ID: {0}", thread.Id);
            }
        }
    }
}
