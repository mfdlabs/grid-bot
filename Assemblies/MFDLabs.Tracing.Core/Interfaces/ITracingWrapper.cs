using System;
using System.Collections.Generic;
using OpenTracing;

namespace MFDLabs.Tracing.Core
{
    public interface ITracingWrapper
    {
        void StartSpan(string operationName, IDictionary<string, string> headersDictionary = null, bool allowNewSpan = false);

        void RecordException(Exception exception);

        bool HasActiveSpan();

        ISpan GetActiveSpan();

        int FinishAllSpans();

        bool FinishSpan();

        IDictionary<string, string> ExtractSpanContextAsHttpHeaders();
    }
}
