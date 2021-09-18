using MFDLabs.Text.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.Client
{
    public class HttpRequestSender : IHttpRequestSender
    {
        public HttpRequestSender(IHttpClient httpClient, IHttpRequestBuilder httpRequestBuilder)
        {
            _HttpClient = httpClient ?? throw new ArgumentNullException("httpClient");
            _HttpRequestBuilder = httpRequestBuilder ?? throw new ArgumentNullException("httpRequestBuilder");
        }

        public void SendRequest(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            _HttpClient.Send(_HttpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters));
        }

        public Task SendRequestAsync(HttpMethod httpMethod, string path, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            return _HttpClient.SendAsync(_HttpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters), cancellationToken);
        }

        public TResponse SendRequest<TResponse>(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            return _HttpClient.Send(_HttpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters)).GetJsonBody<TResponse>();
        }

        public async Task<TResponse> SendRequestAsync<TResponse>(HttpMethod httpMethod, string path, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            return (await _HttpClient.SendAsync(_HttpRequestBuilder.BuildRequest(httpMethod, path, queryStringParameters), cancellationToken).ConfigureAwait(false)).GetJsonBody<TResponse>();
        }

        public void SendRequestWithJsonBody<TRequest>(HttpMethod httpMethod, string path, TRequest requestData, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            _HttpClient.Send(_HttpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters));
        }

        public Task SendRequestWithJsonBodyAsync<TRequest>(HttpMethod httpMethod, string path, TRequest requestData, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null)
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            return _HttpClient.SendAsync(_HttpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters), cancellationToken);
        }

        public TResponse SendRequestWithJsonBody<TRequest, TResponse>(HttpMethod httpMethod, string path, TRequest requestData, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            return _HttpClient.Send(_HttpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters)).GetJsonBody<TResponse>();
        }

        public async Task<TResponse> SendRequestWithJsonBodyAsync<TRequest, TResponse>(HttpMethod httpMethod, string path, TRequest requestData, CancellationToken cancellationToken, IEnumerable<(string, string)> queryStringParameters = null) where TResponse : class
        {
            ValidatePath(path);
            ValidateRequestData(requestData);
            return (await _HttpClient.SendAsync(_HttpRequestBuilder.BuildRequestWithJsonBody(httpMethod, path, requestData, queryStringParameters), cancellationToken).ConfigureAwait(false)).GetJsonBody<TResponse>();
        }

        private void ValidatePath(string path)
        {
            if (path.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value cannot be null or whitespace.", "path");
            }
        }

        private void ValidateRequestData<TRequest>(TRequest requestData)
        {
            if (requestData == null)
            {
                throw new ArgumentNullException("requestData");
            }
        }

        private readonly IHttpClient _HttpClient;

        private readonly IHttpRequestBuilder _HttpRequestBuilder;
    }
}
