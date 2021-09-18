using System;

namespace MFDLabs.Http
{
    public class HttpException : Exception
    {
        public HttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
