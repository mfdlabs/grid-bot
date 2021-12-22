using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Pipeline
{
    public class ExecutionPlan<TInput, TOutput> : IExecutionPlan<TInput, TOutput>
    {
        public IReadOnlyCollection<IPipelineHandler<TInput, TOutput>> Handlers => _handlers.ToArray();

        public void RemoveHandler(int index)
        {
            if (index >= _handlers.Count || index < 0) throw new ArgumentException("index does not exist in handlers.", nameof(index));
            var handler = _handlers[index];
            if (index > 0) _handlers[index - 1].NextHandler = handler.NextHandler;
            _handlers.Remove(handler);
        }
        public void RemoveHandler<T>() 
            where T : IPipelineHandler<TInput, TOutput>
        {
            var idx = GetHandlerIndex<T>();
            if (idx < 0) throw new ArgumentException($"No handler of type {typeof(T)} was found.", nameof(T));
            RemoveHandler(idx);
        }
        public void InsertHandler(int index, IPipelineHandler<TInput, TOutput> handler)
        {
            if (index < 0 || index > _handlers.Count)
                throw new ArgumentException("index is not valid to add to in handlers. Index must be between 0, and the current handlers count.", nameof(index));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.Contains(handler)) 
                throw new ArgumentException("handler is already part of the execution plan. The same instance may only be used in one execution plan once at a time.", nameof(handler));
            if (index > 0) _handlers[index - 1].NextHandler = handler;
            if (index < _handlers.Count) handler.NextHandler = _handlers[index];
            _handlers.Insert(index, handler);
        }
        public void AppendHandler(IPipelineHandler<TInput, TOutput> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            InsertHandler(_handlers.Count, handler);
        }
        public void PrependHandler(IPipelineHandler<TInput, TOutput> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            InsertHandler(0, handler);
        }
        public void AddHandlerAfter<T>(IPipelineHandler<TInput, TOutput> handler) where T : IPipelineHandler<TInput, TOutput>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var idx = GetHandlerIndex<T>();
            if (idx < 0) throw new ArgumentException($"No handler of type {typeof(T)} was found.", nameof(T));
            InsertHandler(idx + 1, handler);
        }
        public void AddHandlerBefore<T>(IPipelineHandler<TInput, TOutput> handler) where T : IPipelineHandler<TInput, TOutput>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var idx = GetHandlerIndex<T>();
            if (idx < 0) throw new ArgumentException($"No handler of type {typeof(T)} was found.", nameof(T));
            InsertHandler(idx, handler);
        }
        public void ClearHandlers() => _handlers.Clear();
        public TOutput Execute(TInput input)
        {
            if (!_handlers.Any()) throw new NoHandlersException();
            var context = new ExecutionContext<TInput, TOutput>(input);
            _handlers.First().Invoke(context);
            return context.Output;
        }
        public async Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken)
        {
            if (!_handlers.Any()) throw new NoHandlersException();
            var context = new ExecutionContext<TInput, TOutput>(input);
            await _handlers.First().InvokeAsync(context, cancellationToken).ConfigureAwait(false);
            return context.Output;
        }
        private int GetHandlerIndex<T>()
        {
            for (int i = 0; i < _handlers.Count; i++) if (_handlers[i] is T) return i;
            return -1;
        }

        private readonly IList<IPipelineHandler<TInput, TOutput>> _handlers = new List<IPipelineHandler<TInput, TOutput>>();

    }
}
