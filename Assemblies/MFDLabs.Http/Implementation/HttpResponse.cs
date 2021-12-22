using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace MFDLabs.Http
{
    public class HttpResponse : IHttpResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusText { get; set; }
        public bool IsSuccessful
        {
            get
            {
                var statusCode = (int)StatusCode;
                return statusCode is >= MinimumSuccessfulStatusCode and <= MaximumSuccessfulStatusCode;
            }
        }
        public Uri Url { get; set; }
        public IHttpResponseHeaders Headers { get; set; }
        public byte[] Body { get; set; }

        public string GetStringBody(Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(Body);
        }
        public T GetJsonBody<T>()
            where T : class 
            => JsonConvert.DeserializeObject<T>(GetStringBody());

        private const int MinimumSuccessfulStatusCode = 200;
        private const int MaximumSuccessfulStatusCode = 299;
    }
}
