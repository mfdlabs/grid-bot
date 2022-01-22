using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class Interleave : IArbiterTask
    {
        public int PendingExclusiveCount
        {
            get
            {
                int result;
                lock (_mutexBranches) result = CountAllPendingItems(_mutexBranches);
                return result;
            }
        }
        public int PendingConcurrentCount
        {
            get
            {
                int result;
                lock (_mutexBranches) result = CountAllPendingItems(_concurrentBranches);
                return result;
            }
        }

        private static int CountAllPendingItems(IEnumerable<ReceiverTask> receivers) =>
            receivers.Select(receiverTask => receiverTask.ArbiterContext as InterleaveReceiverContext)
                .Select(interleaveReceiverContext => interleaveReceiverContext!.PendingItems.Count)
                .Sum();
        public override string ToString()
        {
            var s = _mutexActive switch
            {
                0 when _concurrentActive == 0 => "Idle",
                -1 when _concurrentActive > 0 => "Concurrent Active with Exclusive pending",
                1 when _concurrentActive > 0 => "Exclusive active with Concurrent Active",
                1 when _concurrentActive == 0 => "Exclusive active",
                _ => null
            };
            return string.Format(
                CultureInfo.InvariantCulture,
                "\t{0}({1}) guarding {2} Exclusive and {3} Concurrent branches", 
                GetType().Name, 
                s,
                _mutexBranches.Count, 
                _concurrentBranches.Count
            );
        }

        public Interleave()
        {}
        public Interleave(TeardownReceiverGroup teardown, ExclusiveReceiverGroup mutex, ConcurrentReceiverGroup concurrent)
        {
            foreach (var task in teardown.Branches) 
                task.ArbiterContext = new InterleaveReceiverContext(InterleaveReceivers.Teardown);
            foreach (var task in mutex.Branches) 
                task.ArbiterContext = new InterleaveReceiverContext(InterleaveReceivers.Exclusive);
            foreach (var task in concurrent.Branches) 
                task.ArbiterContext = new InterleaveReceiverContext(InterleaveReceivers.Concurrent);
            _mutexBranches = new List<ReceiverTask>(teardown.Branches);
            foreach (var task in mutex.Branches) _mutexBranches.Add(task);
            _concurrentBranches = new List<ReceiverTask>(concurrent.Branches);
        }
        public Interleave(ExclusiveReceiverGroup mutex, ConcurrentReceiverGroup concurrent) 
            : this(
                new TeardownReceiverGroup(
#if NET40 || NET35
                    new ReceiverTask[0]
#else
                    Array.Empty<ReceiverTask>()
#endif
                ), 
                mutex, 
                concurrent
            )
        {}

        public ITask PartialClone() => throw new NotSupportedException();
        
        public Handler ArbiterCleanupHandler { get; set; }
        public object LinkedIterator
        {
            get => null;
            set => throw new NotSupportedException();
        }
        public ArbiterTaskState ArbiterState { get; private set; }
        public DispatcherQueue TaskQueue { get; set; }
        public IPortElement this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public int PortElementCount => 0;

        public IEnumerator<ITask> Execute()
        {
            ArbiterState = ArbiterTaskState.Active;
            Register();
            return null;
        }
        private void Register()
        {
            lock (_mutexBranches)
            {
                foreach (var task in _mutexBranches) task.Arbiter = this;
                foreach (var task in _concurrentBranches) task.Arbiter = this;
            }
        }
        public void CombineWith(Interleave child)
        {
            if (ArbiterState == ArbiterTaskState.Done) 
                throw new InvalidOperationException("Parent Interleave context is no longer active");
            List<ReceiverTask> mutexBranches;
            List<ReceiverTask> concurrentBranches;
            lock (child._mutexBranches)
            {
                mutexBranches = new List<ReceiverTask>(child._mutexBranches);
                child._mutexBranches = null;
                concurrentBranches = new List<ReceiverTask>(child._concurrentBranches);
                child._concurrentBranches = null;
            }
            lock (_mutexBranches)
            {
                foreach (var task in mutexBranches)
                {
                    _mutexBranches.Add(task);
                    task.Arbiter = this;
                }
                foreach (var task in concurrentBranches)
                {
                    _concurrentBranches.Add(task);
                    if (task.State == ReceiverTaskState.Onetime)
                        throw new InvalidOperationException("Concurrent Receivers must be Reissue");
                    task.Arbiter = this;
                }
            }
        }
        private static void CleanupPending(IEnumerable<ReceiverTask> receivers)
        {
            foreach (var ctx in receivers.Select(receiverTask => receiverTask.ArbiterContext as InterleaveReceiverContext))
            {
                foreach (var t in ctx?.PendingItems!) 
                    t.Item1.Cleanup(t.Item0);
                ctx?.PendingItems.Clear();
            }
        }
        public void Cleanup(ITask winner)
        {
            foreach (var task in _concurrentBranches) task.Cleanup();
            foreach (var task in _mutexBranches) task.Cleanup();
            lock (_mutexBranches)
            {
                CleanupPending(_concurrentBranches);
                CleanupPending(_mutexBranches);
            }
            TaskQueue.Enqueue(winner);
        }
        public bool Evaluate(ReceiverTask receiver, ref ITask deferredTask)
        {
            if (ArbiterState == ArbiterTaskState.Done)
            {
                deferredTask = null;
                return false;
            }
            lock (_mutexBranches)
            {
                if (((InterleaveReceiverContext) receiver.ArbiterContext).ReceiverGroup ==
                    InterleaveReceivers.Teardown &&
                    receiver.State == ReceiverTaskState.Onetime)
                {
                    ArbiterState = ArbiterTaskState.Done;
                    var task = Interlocked.CompareExchange(ref _final, deferredTask, null);
                    if (task != null)
                    {
                        deferredTask = null;
                        return false;
                    }
                }
                var exclusive = ((InterleaveReceiverContext) receiver.ArbiterContext).ReceiverGroup !=
                                    InterleaveReceivers.Concurrent;
                var arbitrateResult = Arbitrate(exclusive);
                if (exclusive)
                {
                    if (arbitrateResult)
                    {
                        if (_final == deferredTask)
                        {
                            _final = null;
                            deferredTask = new Task<ITask>(deferredTask, Cleanup);
                        }
                        else 
                            deferredTask.ArbiterCleanupHandler = ExclusiveFinalizer;
                    }
                    else
                    {
                        if (deferredTask != _final)
                            ((InterleaveReceiverContext) receiver.ArbiterContext).PendingItems.Enqueue(
                                new Tuple<ITask, ReceiverTask>(deferredTask,
                                    receiver));
                        deferredTask = null;
                    }

                    if (deferredTask == null) return true;
                    receiver.TaskQueue.Enqueue(deferredTask);
                    deferredTask = null;
                }
                else if (arbitrateResult) 
                    deferredTask.ArbiterCleanupHandler = ConcurrentFinalizer;
                else
                {
                    ((InterleaveReceiverContext) receiver.ArbiterContext).PendingItems.Enqueue(
                        new Tuple<ITask, ReceiverTask>(deferredTask,
                            receiver));
                    deferredTask = null;
                }
            }
            return true;
        }
        private bool Arbitrate(bool isExclusive)
        {
            if (isExclusive)
            {
                switch (_mutexActive)
                {
                    case 0 when _concurrentActive > 0:
                        _mutexActive = -1;
                        return false;
                    case 0:
                        _mutexActive = 1;
                        return true;
                    case -1 when _concurrentActive == 0:
                        _mutexActive = 1;
                        return true;
                }
            }
            else if (_mutexActive == 0)
            {
                _concurrentActive++;
                return true;
            }
            return false;
        }
        private void ExclusiveFinalizer() => ProcessAllPending(true);
        private void ConcurrentFinalizer() => ProcessAllPending(false);
        private void ProcessAllPending(bool exclusiveJustFinished)
        {
            ITask task = null;
            lock (_mutexBranches)
            {
                if (ArbiterState == ArbiterTaskState.Done)
                {
                    if (_final == null) return;
                    task = (ITask)_final;
                }
            }
            ITask pTask = null;
            lock (_mutexBranches)
            {
                if (exclusiveJustFinished) 
                    _mutexActive = 0;
                else 
                    _concurrentActive--;
                if (task == null) pTask = ProcessPending(true, _mutexBranches);
            }
            if (pTask == null)
            {
                while (true)
                {
                    lock (_mutexBranches) pTask = ProcessPending(false, _concurrentBranches);
                    if (pTask == null) break;
                    pTask.ArbiterCleanupHandler = ConcurrentFinalizer;
                    TaskQueue.Enqueue(pTask);
                }

                if (task == null) return;
                lock (_mutexBranches)
                    if (_concurrentActive == 0 && _mutexActive <= 0) 
                        _final = null;
                if (_final == null) TaskQueue.Enqueue(new Task<ITask>(task, Cleanup));
                return;
            }
            pTask.ArbiterCleanupHandler = ExclusiveFinalizer;
            TaskQueue.Enqueue(pTask);
        }
        private ITask ProcessPending(bool isExclusive, List<ReceiverTask> receivers)
        {
            var taskCount = isExclusive ? _mutexBranches.Count : _concurrentBranches.Count;
            if (taskCount == 0) return null;
            var tCount = taskCount;
            while (--tCount >= 0)
            {
                var idx = isExclusive ? _nextMutexQueueIndex++ : _nextConcurrentQueueIndex++;
                idx %= taskCount;
                var pendingItems = ((InterleaveReceiverContext)receivers[idx].ArbiterContext).PendingItems;
                if (pendingItems.Count <= 0 || !Arbitrate(isExclusive)) continue;
                return pendingItems.Dequeue().Item0;
            }
            return null;
        }
        public ITask TryDequeuePendingTask(InterleaveReceivers receiverMask)
        {
            lock (_mutexBranches)
            {
                if ((receiverMask & InterleaveReceivers.Exclusive) > 0) return DequeuePendingItem(_mutexBranches);
                if ((receiverMask & InterleaveReceivers.Concurrent) > 0) return DequeuePendingItem(_concurrentBranches);
            }
            return null;
        }
        private static ITask DequeuePendingItem(IEnumerable<ReceiverTask> receivers) =>
            (from receiverTask in receivers
                select ((InterleaveReceiverContext) receiverTask.ArbiterContext).PendingItems
                into pendingItems
                where pendingItems.Count > 0
                select pendingItems.Dequeue()
                    .Item0).FirstOrDefault();

        private List<ReceiverTask> _mutexBranches;
        private List<ReceiverTask> _concurrentBranches;
        private object _final;
        private int _mutexActive;
        private int _concurrentActive;
        private int _nextMutexQueueIndex;
        private int _nextConcurrentQueueIndex;
    }
}
