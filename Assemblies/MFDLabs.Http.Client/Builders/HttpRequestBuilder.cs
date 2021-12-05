using System;
using System.Collections.Generic;
using System.Text;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public class HttpRequestBuilder : IHttpRequestBuilder
    {
        public HttpRequestBuilder(string endpoint)
            : this(new HttpRequestBuilderSettings(endpoint))
        { }

        public HttpRequestBuilder(IHttpRequestBuilderSettings httpRequestBuilderSettings) 
            => _HttpRequestBuilderSettings = httpRequestBuilderSettings ?? throw new ArgumentNullException("httpRequestBuilderSettings");

        public IHttpRequest BuildRequest(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            return CreateRequest(httpMethod, path, queryStringParameters);
        }
        public IHttpRequest BuildRequestWithJsonBody<TRequest>(HttpMethod httpMethod, string path, TRequest requestData, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            var request = CreateRequest(httpMethod, path, queryStringParameters);
            request.SetJsonRequestBody(requestData);
            return request;
        }
        private IHttpRequest CreateRequest(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters) 
            => new HttpRequest(httpMethod, CreateUriBuilder(path, queryStringParameters).Uri);
        private UriBuilder CreateUriBuilder(string path, IEnumerable<(string, string)> queryStringParameters)
        {
            var builder = new UriBuilder(_HttpRequestBuilderSettings.Endpoint) { Path = path };
            if (queryStringParameters != null) builder.Query = BuildQueryString(queryStringParameters);
            return builder;
        }
        private string BuildQueryString(IEnumerable<(string, string)> queryStringParameters)
        {
            var builder = new StringBuilder();
            foreach (var (key, value) in queryStringParameters)
            {
                if (key.IsNullOrWhiteSpace()) throw new ArgumentException("Query string parameter key cannot be null or whitespace", nameof(queryStringParameters));
                builder.Append("&");
                if (_HttpRequestBuilderSettings.EncodeQueryParametersEnabled)
                {
                    builder.Append(Uri.EscapeDataString(key));
                    builder.Append("=");
                    builder.Append(Uri.EscapeDataString(value ?? string.Empty));
                }
                else
                {
                    builder.Append(key);
                    builder.Append("=");
                    builder.Append(value ?? string.Empty);
                }
            }
            return builder.ToString();
        }
        private void ValidatePath(string path)
        {
            if (path.IsNullOrWhiteSpace()) throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
        }
        private void ValidateRequestData<TRequest>(TRequest requestData)
        {
            if (requestData == null) throw new ArgumentNullException(nameof(requestData));
        }

        private readonly IHttpRequestBuilderSettings _HttpRequestBuilderSettings;
    }
}
