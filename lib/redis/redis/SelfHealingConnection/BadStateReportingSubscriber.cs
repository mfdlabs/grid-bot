namespace Redis;

using System;
using System.Net;
using System.Threading.Tasks;

using StackExchange.Redis;

public partial class SelfHealingConnectionBuilder
{
    private partial class SelfHealingConnectionMultiplexer
    {
        private class BadStateReportingSubscriber : BadStateReportingBase<ISubscriber>, ISubscriber, IRedis, IRedisAsync
        {
            public override event Action BadStateExceptionOccurred;

            public BadStateReportingSubscriber(ISubscriber decorated)
                : base(decorated)
            {
            }

            public ConnectionMultiplexer Multiplexer => Decorated.Multiplexer;


            public EndPoint IdentifyEndpoint(RedisChannel channel, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.IdentifyEndpoint(channel, flags));


            public Task<EndPoint> IdentifyEndpointAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.IdentifyEndpointAsync(channel, flags));


            public bool IsConnected(RedisChannel channel = default(RedisChannel))
                => DoDecoratedOperation(s => s.IsConnected(channel));


            public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Publish(channel, message, flags));


            public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.PublishAsync(channel, message, flags));


            public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Subscribe(channel, handler, flags));


            public Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.SubscribeAsync(channel, handler, flags));


            public EndPoint SubscribedEndpoint(RedisChannel channel)
                => DoDecoratedOperation(s => s.SubscribedEndpoint(channel));


            public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Unsubscribe(channel, handler, flags));


            public void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.UnsubscribeAll(flags));


            public Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.UnsubscribeAllAsync(flags));


            public Task UnsubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.UnsubscribeAsync(channel, handler, flags));


            public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.Ping(flags));


            public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
                => DoDecoratedOperation(s => s.PingAsync(flags));


            public bool TryWait(Task task)
                => DoDecoratedOperation(t => t.TryWait(task));


            public void Wait(Task task)
                => DoDecoratedOperation(t => t.Wait(task));


            public T Wait<T>(Task<T> task)
                => DoDecoratedOperation(t => t.Wait(task));


            public void WaitAll(params Task[] tasks)
                => DoDecoratedOperation(t => t.WaitAll(tasks));


            protected override void RaiseBadStateExceptionOccurred()
                => BadStateExceptionOccurred?.Invoke();
        }
    }
}
