using OpenTracing;
using System;
using System.Collections.Generic;

namespace MFDLabs.Tracing.Core
{
    public class NoopTracingWrapper : ITracingWrapper
    {
        private NoopTracingWrapper()
        {
        }

        public static ITracingWrapper Instance
        {
            get
            {
                return _Instance.Value;
            }
        }

        public void StartSpan(string operationName, IDictionary<string, string> headersDictionary = null, bool allowNewSpan = false)
        {
        }

        public void RecordException(Exception exception)
        {
        }

        public bool HasActiveSpan()
        {
            return false;
        }

        public ISpan GetActiveSpan()
        {
            return null;
        }

        public int FinishAllSpans()
        {
            return 0;
        }

        public bool FinishSpan()
        {
            return false;
        }

        public IDictionary<string, string> ExtractSpanContextAsHttpHeaders()
        {
            return new Dictionary<string, string>();
        }

        private static readonly Lazy<ITracingWrapper> _Instance = new Lazy<ITracingWrapper>(() => new NoopTracingWrapper());
    }
}
