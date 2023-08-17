namespace FloodCheckers.Redis;

using System;

public interface ISettings
{
    string FloodCheckerRedisEndpointsCsv { get; }
    TimeSpan FloodCheckerMinimumWindowPeriod { get; }
}
