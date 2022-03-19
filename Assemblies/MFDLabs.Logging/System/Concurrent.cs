using System;
using System.Reflection;
using Microsoft.Ccr.Core;

namespace MFDLabs.Logging.Diagnostics
{
    /// <summary>
    /// http://social.msdn.microsoft.com/Forums/en-US/roboticsccr/thread/75f441b6-9eb0-4ce9-bbd2-49505ccb4152/
    /// </summary>
    internal class PatchedDispatcherQueue : DispatcherQueue
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

    /// <summary>
    /// Simple interleave wrapper
    /// </summary>
    internal sealed class Interleaver
    {
        private readonly Port<Action> _exclusive = new Port<Action>();
        private readonly Port<Action> _concurrent = new Port<Action>();

        /// <summary>
        /// DoExclusive
        /// </summary>
        /// <param name="action"></param>
        public void DoExclusive(Action action) => _exclusive.Post(action);

        /// <summary>
        /// DoConcurrent
        /// </summary>
        /// <param name="action"></param>
        public void DoConcurrent(Action action) => _concurrent.Post(action);

        /// <summary>
        /// Construct new interleaver
        /// </summary>
        public Interleaver(DispatcherQueue q)
        {
            Arbiter.Activate(q, Arbiter.Interleave(
                new TeardownReceiverGroup(),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive(true, _exclusive, action => action())
                ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive(true, _concurrent, action => action())
                )
            ));
        }
    }
}
