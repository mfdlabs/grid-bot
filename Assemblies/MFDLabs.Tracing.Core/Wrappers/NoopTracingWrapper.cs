using System;
using System.Collections.Generic;
using OpenTracing;

namespace MFDLabs.Tracing.Core
{
    public class NoopTracingWrapper : ITracingWrapper
    {
        private NoopTracingWrapper()
        { }

        public static ITracingWrapper Instance => _Instance.Value;

        public void StartSpan(string operationName, IDictionary<string, string> headersDictionary = null, bool allowNewSpan = false) { }
        public void RecordException(Exception exception) { }
        public bool HasActiveSpan() => false;
        public ISpan GetActiveSpan() => null;
        public int FinishAllSpans() => 0;
        public bool FinishSpan() => false;
        public IDictionary<string, string> ExtractSpanContextAsHttpHeaders() => new Dictionary<string, string>();

        private static readonly Lazy<ITracingWrapper> _Instance = new Lazy<ITracingWrapper>(() => new NoopTracingWrapper());
    }
}
