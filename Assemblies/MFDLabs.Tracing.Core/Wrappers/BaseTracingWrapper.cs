using System;
using System.Collections.Generic;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace MFDLabs.Tracing.Core
{
    public class BaseTracingWrapper : ITracingWrapper
    {
        public BaseTracingWrapper(ITracer tracer) => _Tracer = tracer;

        public void StartSpan(string operationName, IDictionary<string, string> headersDictionary = null, bool allowNewSpan = false)
        {
            var context = GetSpanContext(headersDictionary);
            if (!(context == null && !allowNewSpan)) _Tracer.BuildSpan(operationName).AsChildOf(context).StartActive(false);
        }
        private ISpanContext GetSpanContext(IDictionary<string, string> headersDictionary)
        {
            var context = _Tracer.Extract(BuiltinFormats.HttpHeaders, new TextMapExtractAdapter(headersDictionary ?? new Dictionary<string, string>()));
            if (context != null)
                return context;
            else
                return HasActiveSpan() ? GetActiveSpan().Context : null;
        }
        public void RecordException(Exception exception)
        {
            if (exception != null)
                if (_Tracer.ActiveSpan != null)
                {
                    _Tracer.ActiveSpan.SetTag(Tags.Error, true);
                    var errorLog = ExtractErrorLog(exception);
                    if (exception.InnerException != null)
                        errorLog["inner.exception"] = exception.InnerException;
                    _Tracer.ActiveSpan.Log(errorLog);
                }
        }
        private static Dictionary<string, object> ExtractErrorLog(Exception exception)
        {
            var errorLogs = new Dictionary<string, object>
            {
                {
                    "error.kind",
                    exception.GetType().FullName
                }
            };
            if (exception.StackTrace != null) errorLogs["stack"] = exception.StackTrace;
            if (exception.Message != null) errorLogs["message"] = exception.Message;
            return errorLogs;
        }
        public bool HasActiveSpan() => _Tracer.ActiveSpan != null;
        public ISpan GetActiveSpan() => _Tracer.ActiveSpan;
        public int FinishAllSpans()
        {
            int spanRunning = 0;
            while (HasActiveSpan()) spanRunning += FinishSpan() ? 1 : 0;
            return spanRunning;
        }
        public bool FinishSpan()
        {
            if (!HasActiveSpan()) 
                return false;
            else
            {
                _Tracer.ScopeManager.Active.Span.Finish();
                _Tracer.ScopeManager.Active.Dispose();
                return true;
            }
        }
        public IDictionary<string, string> ExtractSpanContextAsHttpHeaders()
        {
            var httpHeaders = new Dictionary<string, string>();
            if (!HasActiveSpan()) 
                return httpHeaders;
            else
            {
                _Tracer.Inject(GetActiveSpan().Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(httpHeaders));
                return httpHeaders;
            }
        }

        private readonly ITracer _Tracer;
    }
}
