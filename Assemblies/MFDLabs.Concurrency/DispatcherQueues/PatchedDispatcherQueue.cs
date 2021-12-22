using System.Reflection;
using Microsoft.Ccr.Core;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    /// <summary>
    /// http://social.msdn.microsoft.com/Forums/en-US/roboticsccr/thread/75f441b6-9eb0-4ce9-bbd2-49505ccb4152/
    /// </summary>
    public class PatchedDispatcherQueue : DispatcherQueue
    {
        private static readonly FieldInfo Next = typeof(TaskCommon).GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Previous = typeof(TaskCommon).GetField("_previous", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <inheritdoc/>
        public PatchedDispatcherQueue(string name, Dispatcher dispatcher)
            : base(name, dispatcher)
        {
        }

        /// <inheritdoc/>
        public override bool TryDequeue(out ITask task)
        {
            var result = base.TryDequeue(out task);

            if (!result) return false;

            if (!(task is TaskCommon taskCommon)) return true;
            
            Next.SetValue(taskCommon, null);
            Previous.SetValue(taskCommon, null);

            return true;
        }
    }
}
