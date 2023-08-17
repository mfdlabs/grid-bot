namespace Redis;

using System;

using StackExchange.Redis;

public partial class SelfHealingConnectionBuilder
{
    private partial class SelfHealingConnectionMultiplexer
    {
        private abstract class BadStateReportingBase<T>
            where T : class
        {
            protected T Decorated { get; }

            public abstract event Action BadStateExceptionOccurred;

            protected BadStateReportingBase(T decorated)
            {
                Decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
            }

            protected TResult DoDecoratedOperation<TResult>(Func<T, TResult> operation)
            {
                try
                {
                    return operation(Decorated);
                }
                catch (RedisConnectionException)
                {
                    RaiseBadStateExceptionOccurred();
                    throw;
                }
                catch (RedisTimeoutException)
                {
                    RaiseBadStateExceptionOccurred();
                    throw;
                }
            }

            protected void DoDecoratedOperation(Action<T> operation)
            {
                try
                {
                    operation(Decorated);
                }
                catch (RedisConnectionException)
                {
                    RaiseBadStateExceptionOccurred();
                    throw;
                }
                catch (RedisTimeoutException)
                {
                    RaiseBadStateExceptionOccurred();
                    throw;
                }
            }

            protected abstract void RaiseBadStateExceptionOccurred();
        }
    }
}
