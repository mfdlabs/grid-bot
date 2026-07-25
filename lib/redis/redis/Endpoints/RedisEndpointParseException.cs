namespace Redis;

using System;

internal class RedisEndpointParseException : Exception
{
    public RedisEndpointParseException(string message) : base(message)
    {
    }
}
