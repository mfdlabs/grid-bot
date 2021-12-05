using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http
{
    public abstract class HttpHeaders : IHttpHeaders
    {
        public IReadOnlyList<string> Keys
            => (from headers in Headers
                select headers.Key).ToList();
        public string ContentType
        {
            get
            {
                if (ContentHeaders.ContentType == null)return null;
                return ContentHeaders.ContentType.MediaType;
            }
            set
            {
                if (!value.IsNullOrWhiteSpace()) 
                    ContentHeaders.ContentType = new MediaTypeHeaderValue(value.Split(';').First());
            }
        }

        public HttpHeaders(System.Net.Http.Headers.HttpHeaders httpHeaders, HttpContentHeaders contentHeaders)
        {
            Headers = httpHeaders ?? throw new ArgumentNullException("httpHeaders");
            ContentHeaders = (contentHeaders ?? CreateContentHeaders());
        }
        public HttpHeaders(System.Net.Http.Headers.HttpHeaders httpHeaders)
            : this(httpHeaders, null)
        { }

        public void Add(string name, string value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("name cannot be null or whitespace.", nameof(name));
            Headers.TryAddWithoutValidation(name, value);
        }
        public void AddOrUpdate(string name, string value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("name cannot be null or whitespace.", nameof(name));
            if (Keys.Contains(name, StringComparer.OrdinalIgnoreCase)) Remove(name);
            Add(name, value);
        }
        public ICollection<string> Get(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("name cannot be null or whitespace.", nameof(name));
            if (!Keys.Contains(name, StringComparer.OrdinalIgnoreCase)) return new string[0];
            return Headers.GetValues(name).ToArray();
        }
        public bool Remove(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentException("name cannot be null or whitespace.", nameof(name));
            return Headers.Remove(name);
        }
        private HttpContentHeaders CreateContentHeaders() => new ByteArrayContent(new byte[0]).Headers;

        protected readonly System.Net.Http.Headers.HttpHeaders Headers;
        protected readonly HttpContentHeaders ContentHeaders;
    }
}
