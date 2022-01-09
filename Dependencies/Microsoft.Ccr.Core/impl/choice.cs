using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    public class Choice : IArbiterTask, ITask
    {
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "\tChoice ({1}) with {0} branches",
                _branches.Count,
                Enum.GetName(typeof(Choice.ChoiceStage), _stage)
            );
        }

        public Choice(params ReceiverTask[] branches)
        {
            if (branches == null) throw new ArgumentNullException(nameof(branches));
            if (_stage != 0) throw new InvalidOperationException(Resource.ChoiceAlreadyActiveException);
            
            if (branches.Any(receiverTask => receiverTask.State == ReceiverTaskState.Persistent)) 
                throw new ArgumentOutOfRangeException(nameof(branches), Resource.ChoiceBranchesCannotBePersisted);
            _branches = new List<ReceiverTask>(branches);
        }

        public ITask PartialClone() => throw new NotSupportedException();

        public DispatcherQueue TaskQueue { get; set; }
        public Handler ArbiterCleanupHandler { get; set; }
        public object LinkedIterator { get; set; }
        public ArbiterTaskState ArbiterState
        {
            get
            {
                if (_stage >= 2) return ArbiterTaskState.Done;
                return _stage == 0 ? ArbiterTaskState.Created : ArbiterTaskState.Active;
            }
        }
        public IPortElement this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public int PortElementCount => 0;

        public IEnumerator<ITask> Execute()
        {
            _stage++;
            foreach (var receiverTask in _branches) 
                receiverTask.Arbiter = this;
            return null;
        }
        private void Cleanup(ITask winner)
        {
            foreach (var receiverTask in _branches) receiverTask.Cleanup();
            winner.LinkedIterator = LinkedIterator;
            winner.ArbiterCleanupHandler = ArbiterCleanupHandler;
            TaskQueue.Enqueue(winner);
        }
        public bool Evaluate(ReceiverTask receiver, ref ITask deferredTask)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            var choiceStage = (ChoiceStage)Interlocked.Increment(ref _stage);
            if (choiceStage == ChoiceStage.Commited)
            {
                deferredTask = new Task<ITask>(deferredTask, Cleanup);
                return true;
            }
            deferredTask = null;
            return false;
        }

        private readonly List<ReceiverTask> _branches;
        private int _stage;

        private enum ChoiceStage
        {
            Initialized,
            Pending,
            Commited,
            PostCommit0,
            PostCommit1,
            PostCommit2,
            PostCommit3,
            PostCommit4
        }
    }
}
