using System;
using System.Net.Http;

namespace MFDLabs.Http
{
    public interface IHttpRequest
    {
        HttpMethod Method { get; set; }

        Uri Url { get; set; }

        IHttpRequestHeaders Headers { get; }

        HttpContent Body { get; set; }

        TimeSpan? Timeout { get; set; }

        void SetJsonRequestBody(object requestBody);
    }
}
