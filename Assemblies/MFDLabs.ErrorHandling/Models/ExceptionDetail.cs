using System;

namespace MFDLabs.ErrorHandling
{
    public class ExceptionDetail
    {
        private readonly Exception _Exception;

        public ExceptionDetail(Exception ex)
        {
            _Exception = ex;
        }

        public override string ToString()
        {
            return string.Format(
                _DefaultErrorFormat,
                _Exception.GetType().FullName,
                _Exception.Message != null ? _Exception.Message.ToString() : "",
                _Exception.InnerException != null ? _Exception.InnerException.ToString() : "",
                _Exception.StackTrace != null ? _Exception.StackTrace.ToString() : "",
                _Exception.Source != null ? _Exception.Source.ToString() : "",
                _Exception.TargetSite != null ? _Exception.TargetSite.ToString() : "",
                _Exception.Data != null ? _Exception.Data.ToString() : ""
            );
        }

        private const string _DefaultErrorFormat = "\r\nError Type: {0}\r\nError Detail: {1}\r\nInner Exception: {2}\r\nException Stack Trace: \r\n{3}\r\nException Source: {4}\r\nException TargetSite: {5}\r\nException Data: {6}";
    }
}
