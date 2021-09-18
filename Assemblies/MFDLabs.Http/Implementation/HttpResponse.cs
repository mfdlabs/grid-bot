using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;

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
                return statusCode >= _MinimumSuccessfulStatusCode && statusCode <= _MaximumSuccessfulStatusCode;
            }
        }

        public Uri Url { get; set; }

        public IHttpResponseHeaders Headers { get; set; }

        public byte[] Body { get; set; }

        public string GetStringBody(Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            return encoding.GetString(Body);
        }

        public T GetJsonBody<T>() where T : class
        {
            return JsonConvert.DeserializeObject<T>(GetStringBody(null));
        }

        private const int _MinimumSuccessfulStatusCode = 200;

        private const int _MaximumSuccessfulStatusCode = 299;
    }
}
