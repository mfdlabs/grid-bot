using Microsoft.Ccr.Core.Arbiters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Microsoft.Ccr.Core
{
    public class Interleave : IArbiterTask, ITask
    {
        public int PendingExclusiveCount
        {
            get
            {
                int result;
                lock (this._mutexBranches)
                {
                    result = this.CountAllPendingItems(this._mutexBranches);
                }
                return result;
            }
        }

        public int PendingConcurrentCount
        {
            get
            {
                int result;
                lock (this._mutexBranches)
                {
                    result = this.CountAllPendingItems(this._concurrentBranches);
                }
                return result;
            }
        }

        private int CountAllPendingItems(List<ReceiverTask> receivers)
        {
            int num = 0;
            foreach (ReceiverTask receiverTask in receivers)
            {
                InterleaveReceiverContext interleaveReceiverContext = receiverTask.ArbiterContext as InterleaveReceiverContext;
                num += interleaveReceiverContext.PendingItems.Count;
            }
            return num;
        }

        public override string ToString()
        {
            string text = null;
            if (this._mutexActive == 0 && this._concurrentActive == 0)
            {
                text = "Idle";
            }
            else if (this._mutexActive == -1 && this._concurrentActive > 0)
            {
                text = "Concurrent Active with Exclusive pending";
            }
            else if (this._mutexActive == 1 && this._concurrentActive > 0)
            {
                text = "Exclusive active with Concurrent Active";
            }
            else if (this._mutexActive == 1 && this._concurrentActive == 0)
            {
                text = "Exclusive active";
            }
            return string.Format(CultureInfo.InvariantCulture, "\t{0}({1}) guarding {2} Exclusive and {3} Concurrent branches", new object[]
            {
                base.GetType().Name,
                text,
                this._mutexBranches.Count,
                this._concurrentBranches.Count
            });
        }

        public Interleave()
        {
        }

        public Interleave(TeardownReceiverGroup teardown, ExclusiveReceiverGroup mutex, ConcurrentReceiverGroup concurrent)
        {
            foreach (ReceiverTask receiverTask in teardown._branches)
            {
                receiverTask.ArbiterContext = new InterleaveReceiverContext(InterleaveReceivers.Teardown);
            }
            foreach (ReceiverTask receiverTask2 in mutex.Branches)
            {
                receiverTask2.ArbiterContext = new InterleaveReceiverContext(InterleaveReceivers.Exclusive);
            }
            foreach (ReceiverTask receiverTask3 in concurrent.Branches)
            {
                receiverTask3.ArbiterContext = new InterleaveReceiverContext(InterleaveReceivers.Concurrent);
            }
            this._mutexBranches = new List<ReceiverTask>(teardown._branches);
            foreach (ReceiverTask item in mutex.Branches)
            {
                this._mutexBranches.Add(item);
            }
            this._concurrentBranches = new List<ReceiverTask>(concurrent.Branches);
        }

        public Interleave(ExclusiveReceiverGroup mutex, ConcurrentReceiverGroup concurrent) : this(new TeardownReceiverGroup(new ReceiverTask[0]), mutex, concurrent)
        {
        }

        public ITask PartialClone()
        {
            throw new NotSupportedException();
        }

        public Handler ArbiterCleanupHandler
        {
            get
            {
                return this._ArbiterCleanupHandler;
            }
            set
            {
                this._ArbiterCleanupHandler = value;
            }
        }

        public object LinkedIterator
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public ArbiterTaskState ArbiterState
        {
            get
            {
                return this._state;
            }
        }

        public DispatcherQueue TaskQueue
        {
            get
            {
                return this._dispatcherQueue;
            }
            set
            {
                this._dispatcherQueue = value;
            }
        }

        public IPortElement this[int index]
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public int PortElementCount
        {
            get
            {
                return 0;
            }
        }

        public IEnumerator<ITask> Execute()
        {
            this._state = ArbiterTaskState.Active;
            this.Register();
            return null;
        }

        private void Register()
        {
            lock (this._mutexBranches)
            {
                foreach (ReceiverTask receiverTask in this._mutexBranches)
                {
                    receiverTask.Arbiter = this;
                }
                foreach (ReceiverTask receiverTask2 in this._concurrentBranches)
                {
                    receiverTask2.Arbiter = this;
                }
            }
        }

        public void CombineWith(Interleave child)
        {
            if (this._state == ArbiterTaskState.Done)
            {
                throw new InvalidOperationException("Parent Interleave context is no longer active");
            }
            List<ReceiverTask> list = null;
            List<ReceiverTask> list2 = null;
            lock (child._mutexBranches)
            {
                list = new List<ReceiverTask>(child._mutexBranches);
                child._mutexBranches = null;
                list2 = new List<ReceiverTask>(child._concurrentBranches);
                child._concurrentBranches = null;
            }
            lock (this._mutexBranches)
            {
                foreach (ReceiverTask receiverTask in list)
                {
                    this._mutexBranches.Add(receiverTask);
                    receiverTask.Arbiter = this;
                }
                foreach (ReceiverTask receiverTask2 in list2)
                {
                    this._concurrentBranches.Add(receiverTask2);
                    if (receiverTask2.State == ReceiverTaskState.Onetime)
                    {
                        throw new InvalidOperationException("Concurrent Receivers must be Reissue");
                    }
                    receiverTask2.Arbiter = this;
                }
            }
        }

        private void CleanupPending(List<ReceiverTask> receivers)
        {
            foreach (ReceiverTask receiverTask in receivers)
            {
                InterleaveReceiverContext interleaveReceiverContext = receiverTask.ArbiterContext as InterleaveReceiverContext;
                foreach (Tuple<ITask, ReceiverTask> tuple in interleaveReceiverContext.PendingItems)
                {
                    tuple.Item1.Cleanup(tuple.Item0);
                }
                interleaveReceiverContext.PendingItems.Clear();
            }
        }

        public void Cleanup(ITask winner)
        {
            foreach (ReceiverTask receiverTask in this._concurrentBranches)
            {
                receiverTask.Cleanup();
            }
            foreach (ReceiverTask receiverTask2 in this._mutexBranches)
            {
                receiverTask2.Cleanup();
            }
            lock (this._mutexBranches)
            {
                this.CleanupPending(this._concurrentBranches);
                this.CleanupPending(this._mutexBranches);
            }
            this._dispatcherQueue.Enqueue(winner);
        }

        public bool Evaluate(ReceiverTask receiver, ref ITask deferredTask)
        {
            if (this._state == ArbiterTaskState.Done)
            {
                deferredTask = null;
                return false;
            }
            lock (this._mutexBranches)
            {
                if (((InterleaveReceiverContext)receiver.ArbiterContext).ReceiverGroup == InterleaveReceivers.Teardown && receiver.State == ReceiverTaskState.Onetime)
                {
                    this._state = ArbiterTaskState.Done;
                    object obj = Interlocked.CompareExchange(ref this._final, deferredTask, null);
                    if (obj != null)
                    {
                        deferredTask = null;
                        return false;
                    }
                }
                bool flag = ((InterleaveReceiverContext)receiver.ArbiterContext).ReceiverGroup != InterleaveReceivers.Concurrent;
                bool flag2 = this.Arbitrate(flag);
                if (flag)
                {
                    if (flag2)
                    {
                        if (this._final == deferredTask)
                        {
                            this._final = null;
                            deferredTask = new Task<ITask>(deferredTask, new Handler<ITask>(this.Cleanup));
                        }
                        else
                        {
                            deferredTask.ArbiterCleanupHandler = new Handler(this.ExclusiveFinalizer);
                        }
                    }
                    else
                    {
                        if (deferredTask != this._final)
                        {
                            ((InterleaveReceiverContext)receiver.ArbiterContext).PendingItems.Enqueue(new Tuple<ITask, ReceiverTask>(deferredTask, receiver));
                        }
                        deferredTask = null;
                    }
                    if (deferredTask != null)
                    {
                        receiver.TaskQueue.Enqueue(deferredTask);
                        deferredTask = null;
                    }
                }
                else if (flag2)
                {
                    deferredTask.ArbiterCleanupHandler = new Handler(this.ConcurrentFinalizer);
                }
                else
                {
                    ((InterleaveReceiverContext)receiver.ArbiterContext).PendingItems.Enqueue(new Tuple<ITask, ReceiverTask>(deferredTask, receiver));
                    deferredTask = null;
                }
            }
            return true;
        }

        private bool Arbitrate(bool IsExclusive)
        {
            if (IsExclusive)
            {
                if (this._mutexActive == 0)
                {
                    if (this._concurrentActive > 0)
                    {
                        this._mutexActive = -1;
                        return false;
                    }
                    this._mutexActive = 1;
                    return true;
                }
                else if (this._mutexActive == -1 && this._concurrentActive == 0)
                {
                    this._mutexActive = 1;
                    return true;
                }
            }
            else if (this._mutexActive == 0)
            {
                this._concurrentActive++;
                return true;
            }
            return false;
        }

        private void ExclusiveFinalizer()
        {
            this.ProcessAllPending(true);
        }

        private void ConcurrentFinalizer()
        {
            this.ProcessAllPending(false);
        }

        private void ProcessAllPending(bool exclusiveJustFinished)
        {
            ITask task = null;
            lock (this._mutexBranches)
            {
                if (this._state == ArbiterTaskState.Done)
                {
                    if (this._final == null)
                    {
                        return;
                    }
                    task = (ITask)this._final;
                }
            }
            ITask task2 = null;
            lock (this._mutexBranches)
            {
                if (exclusiveJustFinished)
                {
                    this._mutexActive = 0;
                }
                else
                {
                    this._concurrentActive--;
                }
                if (task == null)
                {
                    task2 = this.ProcessPending(true, this._mutexBranches);
                }
            }
            if (task2 == null)
            {
                for (; ; )
                {
                    lock (this._mutexBranches)
                    {
                        task2 = this.ProcessPending(false, this._concurrentBranches);
                    }
                    if (task2 == null)
                    {
                        break;
                    }
                    task2.ArbiterCleanupHandler = new Handler(this.ConcurrentFinalizer);
                    this._dispatcherQueue.Enqueue(task2);
                }
                if (task != null)
                {
                    lock (this._mutexBranches)
                    {
                        if (this._concurrentActive == 0 && this._mutexActive <= 0)
                        {
                            this._final = null;
                        }
                    }
                    if (this._final == null && task != null)
                    {
                        this._dispatcherQueue.Enqueue(new Task<ITask>(task, new Handler<ITask>(this.Cleanup)));
                    }
                }
                return;
            }
            task2.ArbiterCleanupHandler = new Handler(this.ExclusiveFinalizer);
            this._dispatcherQueue.Enqueue(task2);
        }

        private ITask ProcessPending(bool IsExclusive, List<ReceiverTask> receivers)
        {
            int num = IsExclusive ? this._mutexBranches.Count : this._concurrentBranches.Count;
            if (num == 0)
            {
                return null;
            }
            int num2 = num;
            while (--num2 >= 0)
            {
                int num3 = IsExclusive ? this._nextMutexQueueIndex++ : this._nextConcurrentQueueIndex++;
                num3 %= num;
                Queue<Tuple<ITask, ReceiverTask>> pendingItems = ((InterleaveReceiverContext)receivers[num3].ArbiterContext).PendingItems;
                if (pendingItems.Count > 0 && this.Arbitrate(IsExclusive))
                {
                    Tuple<ITask, ReceiverTask> tuple = pendingItems.Dequeue();
                    return tuple.Item0;
                }
            }
            return null;
        }

        public ITask TryDequeuePendingTask(InterleaveReceivers receiverMask)
        {
            lock (this._mutexBranches)
            {
                if ((receiverMask & InterleaveReceivers.Exclusive) > (InterleaveReceivers)0)
                {
                    return Interleave.DequeuePendingItem(this._mutexBranches);
                }
                if ((receiverMask & InterleaveReceivers.Concurrent) > (InterleaveReceivers)0)
                {
                    return Interleave.DequeuePendingItem(this._concurrentBranches);
                }
            }
            return null;
        }

        private static ITask DequeuePendingItem(List<ReceiverTask> receivers)
        {
            foreach (ReceiverTask receiverTask in receivers)
            {
                Queue<Tuple<ITask, ReceiverTask>> pendingItems = ((InterleaveReceiverContext)receiverTask.ArbiterContext).PendingItems;
                if (pendingItems.Count > 0)
                {
                    return pendingItems.Dequeue().Item0;
                }
            }
            return null;
        }

        private ArbiterTaskState _state;

        private List<ReceiverTask> _mutexBranches;

        private List<ReceiverTask> _concurrentBranches;

        private object _final;

        private int _mutexActive;

        private int _concurrentActive;

        private Handler _ArbiterCleanupHandler;

        private DispatcherQueue _dispatcherQueue;

        private int _nextMutexQueueIndex;

        private int _nextConcurrentQueueIndex;
    }
}
