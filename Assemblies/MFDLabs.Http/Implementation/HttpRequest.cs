using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;

namespace MFDLabs.Http
{
    public class HttpRequest : IHttpRequest
    {
        public HttpMethod Method { get; set; }

        public Uri Url { get; set; }

        public IHttpRequestHeaders Headers { get; set; }

        [ExcludeFromCodeCoverage]
        public HttpContent Body { get; set; }

        public TimeSpan? Timeout { get; set; }

        public HttpRequest(HttpMethod method, Uri url)
        {
            Method = method;
            Url = url ?? throw new ArgumentNullException("url");
            Headers = new HttpRequestHeaders();
        }

        public void SetJsonRequestBody(object requestBody)
        {
            if (requestBody == null)
            {
                throw new ArgumentNullException("requestBody");
            }
            Body = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody)));
            Headers.ContentType = _JsonContentType;
        }

        private const string _JsonContentType = "application/json";
    }
}
