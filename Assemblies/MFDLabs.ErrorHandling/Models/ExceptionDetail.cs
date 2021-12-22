using System;

namespace MFDLabs.ErrorHandling
{
    public class ExceptionDetail
    {
        private readonly Exception _exception;

        public ExceptionDetail(Exception ex) => _exception = ex;

        public override string ToString() =>
            string.Format(
                DefaultErrorFormat,
                _exception.GetType().FullName,
                _exception.Message ?? "",
                _exception.InnerException != null ? _exception.InnerException.ToString() : "",
                _exception.StackTrace ?? "",
                _exception.Source ?? "",
                _exception.TargetSite != null ? _exception.TargetSite.ToString() : "",
                _exception.Data
            );

        private const string DefaultErrorFormat = "\r\nError Type: {0}\r\nError Detail: {1}\r\nInner Exception: {2}\r\nException Stack Trace: \r\n{3}\r\nException Source: {4}\r\nException TargetSite: {5}\r\nException Data: {6}";
    }
}
