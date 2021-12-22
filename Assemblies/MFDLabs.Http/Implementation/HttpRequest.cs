using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

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
            Url = url ?? throw new ArgumentNullException(nameof(url));
            Headers = new HttpRequestHeaders();
        }

        public void SetJsonRequestBody(object requestBody)
        {
            if (requestBody == null) throw new ArgumentNullException(nameof(requestBody));
            Body = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody)));
            Headers.ContentType = JsonContentType;
        }

        private const string JsonContentType = "application/json";
    }
}
