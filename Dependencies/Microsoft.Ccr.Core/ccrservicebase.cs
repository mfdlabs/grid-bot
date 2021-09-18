﻿using System;

namespace Microsoft.Ccr.Core
{
    public class CcrServiceBase
    {
        protected DispatcherQueue TaskQueue
        {
            get
            {
                return this.dispatcherQueue;
            }
            set
            {
                this.dispatcherQueue = value;
            }
        }

        protected CcrServiceBase()
        {
        }

        protected CcrServiceBase(DispatcherQueue dispatcherQueue) : this()
        {
            this.dispatcherQueue = dispatcherQueue;
        }

        public void Activate<T>(params T[] tasks) where T : ITask
        {
            foreach (T task in tasks)
            {
                this.dispatcherQueue.Enqueue(task);
            }
        }

        public static void EmptyHandler<T>(T message)
        {
        }

        protected Port<DateTime> TimeoutPort(int milliseconds)
        {
            return this.TimeoutPort(new TimeSpan(0, 0, 0, 0, milliseconds));
        }

        protected Port<DateTime> TimeoutPort(TimeSpan ts)
        {
            Port<DateTime> port = new Port<DateTime>();
            this.TaskQueue.EnqueueTimer(ts, port);
            return port;
        }

        protected void Spawn(Handler handler)
        {
            this.TaskQueue.Enqueue(new Task(handler));
        }

        protected void SpawnIterator(IteratorHandler handler)
        {
            this.TaskQueue.Enqueue(new IterativeTask(handler));
        }

        protected void Spawn<T0>(T0 t0, Handler<T0> handler)
        {
            this.TaskQueue.Enqueue(new Task<T0>(t0, handler));
        }

        protected void SpawnIterator<T0>(T0 t0, IteratorHandler<T0> handler)
        {
            this.TaskQueue.Enqueue(new IterativeTask<T0>(t0, handler));
        }

        protected void Spawn<T0, T1>(T0 t0, T1 t1, Handler<T0, T1> handler)
        {
            this.TaskQueue.Enqueue(new Task<T0, T1>(t0, t1, handler));
        }

        protected void SpawnIterator<T0, T1>(T0 t0, T1 t1, IteratorHandler<T0, T1> handler)
        {
            this.TaskQueue.Enqueue(new IterativeTask<T0, T1>(t0, t1, handler));
        }

        protected void Spawn<T0, T1, T2>(T0 t0, T1 t1, T2 t2, Handler<T0, T1, T2> handler)
        {
            this.TaskQueue.Enqueue(new Task<T0, T1, T2>(t0, t1, t2, handler));
        }

        protected void SpawnIterator<T0, T1, T2>(T0 t0, T1 t1, T2 t2, IteratorHandler<T0, T1, T2> handler)
        {
            this.TaskQueue.Enqueue(new IterativeTask<T0, T1, T2>(t0, t1, t2, handler));
        }

        private DispatcherQueue dispatcherQueue;
    }
}
