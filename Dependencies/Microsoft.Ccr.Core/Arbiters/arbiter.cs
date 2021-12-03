using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;

namespace Microsoft.Ccr.Core
{
    public static class Arbiter
    {
        public static void ExecuteNow(DispatcherQueue dispatcherQueue, ITask task)
        {
            task.TaskQueue = dispatcherQueue;
            TaskExecutionWorker.ExecuteInCurrentThreadContext(task);
        }
        public static void Activate(DispatcherQueue dispatcherQueue, params ITask[] arbiter)
        {
            if (dispatcherQueue == null) throw new ArgumentNullException(nameof(dispatcherQueue));
            if (arbiter == null) throw new ArgumentNullException(nameof(arbiter));
            foreach (var task in arbiter) dispatcherQueue.Enqueue(task);
        }

        public static ITask FromHandler(Handler handler) => new Task(handler);
        public static ITask FromIteratorHandler(IteratorHandler handler) => new IterativeTask(handler);
        public static ITask ExecuteToCompletion(DispatcherQueue dispatcherQueue, ITask task)
        {
            var done = new Port<EmptyValue>();
            if (task.ArbiterCleanupHandler != null) throw new InvalidOperationException(Resource1.TaskAlreadyHasFinalizer);
            task.ArbiterCleanupHandler = () => done.Post(EmptyValue.SharedInstance);
            dispatcherQueue.Enqueue(task);
            return Receive(false, done, (e) => {});
        }
        public static void ExecuteToCompletion(DispatcherQueue dispatcherQueue, ITask task, Port<EmptyValue> donePort)
        {
            if (task.ArbiterCleanupHandler != null) throw new InvalidOperationException(Resource1.TaskAlreadyHasFinalizer);
            task.ArbiterCleanupHandler = () => donePort.Post(EmptyValue.SharedInstance);
            dispatcherQueue.Enqueue(task);
        }
        public static Receiver<T> Receive<T>(bool persist, Port<T> port, Handler<T> handler) 
            => new Receiver<T>(persist, port, null, new Task<T>(handler));
        public static Receiver<T> ReceiveFromPortSet<T>(bool persist, IPortSet portSet, Handler<T> handler) 
            => new Receiver<T>(persist, (IPortReceive)portSet[typeof(T)], null, new Task<T>(handler));
        public static Receiver<T> Receive<T>(bool persist, Port<T> port, Handler<T> handler, Predicate<T> predicate) 
            => new Receiver<T>(persist, port, predicate, new Task<T>(handler));
        public static Receiver<T> ReceiveFromPortSet<T>(bool persist, IPortSet portSet, Handler<T> handler, Predicate<T> predicate) 
            => new Receiver<T>(persist, (IPortReceive)portSet[typeof(T)], predicate, new Task<T>(handler));
        public static Receiver<T> ReceiveWithIterator<T>(bool persist, Port<T> port, IteratorHandler<T> handler) 
            => new Receiver<T>(persist, port, null, new IterativeTask<T>(handler));
        public static Receiver<T> ReceiveWithIteratorFromPortSet<T>(bool persist, IPortSet portSet, IteratorHandler<T> handler) 
            => new Receiver<T>(persist, (IPortReceive)portSet[typeof(T)], null, new IterativeTask<T>(handler));
        public static Receiver<T> ReceiveWithIterator<T>(bool persist, Port<T> port, IteratorHandler<T> handler, Predicate<T> predicate) 
            => new Receiver<T>(persist, port, predicate, new IterativeTask<T>(handler));
        public static Receiver<T> ReceiveWithIteratorFromPortSet<T>(bool persist, IPortSet portSet, IteratorHandler<T> handler, Predicate<T> predicate) 
            => new Receiver<T>(persist, (IPortReceive)portSet[typeof(T)], predicate, new IterativeTask<T>(handler));
        public static JoinReceiver JoinedReceive<T0, T1>(bool persist, Port<T0> port0, Port<T1> port1, Handler<T0, T1> handler) 
            => new JoinReceiver(persist, new Task<T0, T1>(handler), port0, port1);
        public static JoinReceiver JoinedReceiveWithIterator<T0, T1>(bool persist, Port<T0> port0, Port<T1> port1, IteratorHandler<T0, T1> handler) 
            => new JoinReceiver(persist, new IterativeTask<T0, T1>(handler), port0, port1);
        public static JoinSinglePortReceiver MultipleItemReceive<T>(bool persist, Port<T> port, int itemCount, VariableArgumentHandler<T> handler) 
            => new JoinSinglePortReceiver(persist, new VariableArgumentTask<T>(itemCount, handler), port, itemCount);
        public static JoinReceiver MultiplePortReceive<T>(bool persist, Port<T>[] ports, VariableArgumentHandler<T> handler) 
            => new JoinReceiver(persist, new VariableArgumentTask<T>(ports.Length, handler), ports);

        public static MultipleItemReceiver MultipleItemReceive<T>(VariableArgumentHandler<T> handler, params Port<T>[] ports)
        {
            if (ports == null) throw new ArgumentNullException(nameof(ports));
            if (ports.Length == 0) throw new ArgumentOutOfRangeException(nameof(ports));
            return new MultipleItemReceiver(new VariableArgumentTask<T>(ports.Length, handler), ports);
        }
        public static MultipleItemGather MultipleItemReceive<T0, T1>(PortSet<T0, T1> portSet, int totalItemCount, Handler<ICollection<T0>, ICollection<T1>> handler)
        {
            void cleanup(ICollection[] res)
            {
                var items = new List<T0>(res[0].Count);
                var items2 = new List<T1>(res[1].Count);
                foreach (object obj in res[0])
                {
                    var item = (T0)((object)obj);
                    items.Add(item);
                }
                foreach (object obj2 in res[1])
                {
                    var item = (T1)((object)obj2);
                    items2.Add(item);
                }
                handler(items, items2);
            }
            return new MultipleItemGather(
                new Type[] { typeof(T0), typeof(T1) },
                new IPortReceive[] { portSet.P0, portSet.P1 },
                totalItemCount,
                cleanup
            );
        }
        public static Interleave Interleave(TeardownReceiverGroup teardown, ExclusiveReceiverGroup exclusive, ConcurrentReceiverGroup concurrent) 
            => new Interleave(teardown, exclusive, concurrent);
        public static Choice Choice(params ReceiverTask[] receivers) 
            => new Choice(receivers);
        public static Choice Choice(IPortSet portSet) 
            => PortSet.ImplicitChoiceOperator(portSet);
        public static Choice Choice<T0, T1>(PortSet<T0, T1> resultPort, Handler<T0> handler0, Handler<T1> handler1) 
            => new Choice(resultPort.P0.Receive(handler0), resultPort.P1.Receive(handler1));
    }
}
