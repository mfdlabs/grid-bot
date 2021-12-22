using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public class HttpRequestSender : IHttpRequestSender
    {
        public HttpRequestSender(IHttpClient httpClient, IHttpRequestBuilder httpRequestBuilder)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpRequestBuilder = httpRequestBuilder ?? throw new ArgumentNullException(nameof(httpRequestBuilder));
        }

        public void SendRequest(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            _httpClient.Send(_httpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters));
        }
        public Task SendRequestAsync(HttpMethod httpMethod, string path, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            return _httpClient.SendAsync(_httpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters), cancellationToken);
        }
        public TResponse SendRequest<TResponse>(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            return _httpClient.Send(_httpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters)).GetJsonBody<TResponse>();
        }
        public async Task<TResponse> SendRequestAsync<TResponse>(HttpMethod httpMethod, string path, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            return (await _httpClient.SendAsync(_httpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters), cancellationToken).ConfigureAwait(false)).GetJsonBody<TResponse>();
        }
        public void SendRequestWithJsonBody<TRequest>(HttpMethod httpMethod, string path, TRequest requestData, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            _httpClient.Send(_httpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters));
        }
        public Task SendRequestWithJsonBodyAsync<TRequest>(HttpMethod httpMethod, string path, TRequest requestData, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            return _httpClient.SendAsync(_httpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters), cancellationToken);
        }
        public TResponse SendRequestWithJsonBody<TRequest, TResponse>(HttpMethod httpMethod, string path, TRequest requestData, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            return _httpClient.Send(_httpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters)).GetJsonBody<TResponse>();
        }
        public async Task<TResponse> SendRequestWithJsonBodyAsync<TRequest, TResponse>(HttpMethod httpMethod, string path, TRequest requestData, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            return (await _httpClient.SendAsync(_httpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters), cancellationToken).ConfigureAwait(false)).GetJsonBody<TResponse>();
        }
        private static void ValidatePath(string path)
        {
            if (path.IsNullOrWhiteSpace()) throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
        }
        private static void ValidateRequestData<TRequest>(TRequest requestData)
        {
            if (requestData == null) throw new ArgumentNullException(nameof(requestData));
        }

        private readonly IHttpClient _httpClient;
        private readonly IHttpRequestBuilder _httpRequestBuilder;
    }
}
