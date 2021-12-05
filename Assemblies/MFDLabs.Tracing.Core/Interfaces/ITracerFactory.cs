using System;
using OpenTracing;

namespace MFDLabs.Tracing.Core
{
    public interface ITracerFactory
    {
        event EventHandler TracerOutdated;

        ITracingWrapper GetTracingWrapper();
        ITracer GetTracer();
    }
}
