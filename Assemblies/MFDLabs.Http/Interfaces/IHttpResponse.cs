using System;
using System.Net;
using System.Text;

namespace MFDLabs.Http
{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; }
        string StatusText { get; }
        bool IsSuccessful { get; }
        Uri Url { get; }
        IHttpResponseHeaders Headers { get; }
        byte[] Body { get; set; }

        string GetStringBody(Encoding encoding = null);
        T GetJsonBody<T>() where T : class;
    }
}
