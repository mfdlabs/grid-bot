﻿namespace Instrumentation
{
    public interface IRateOfCountsPerSecondCounter
    {
        void IncrementBy(long eventCount);
        void Increment();
    }
}
