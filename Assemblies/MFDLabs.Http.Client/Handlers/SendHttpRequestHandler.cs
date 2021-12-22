using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public sealed class SendHttpRequestHandler : PipelineHandler<IHttpRequest, IHttpResponse>, IDisposable
    {
        public SendHttpRequestHandler(CookieContainer cookieContainer, IHttpClientSettings httpClientSettings, IHttpMessageHandlerBuilder httpMessageHandlerBuilder)
        {
            _cookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
            _httpClientSettings = httpClientSettings ?? throw new ArgumentNullException(nameof(httpClientSettings));
            _httpMessageHandlerBuilder = httpMessageHandlerBuilder ?? throw new ArgumentNullException(nameof(httpMessageHandlerBuilder));
            RefreshHttpClient();
        }

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            try
            {
                using var response = (HttpWebResponse)BuildHttpWebRequest(context.Input).GetResponse();
                context.Output = BuildHttpResponse(response);
                if (HandleCsrfAndReInvoke(context)) return;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout) throw new HttpException("The operation has timed out", ex);
                if (!(ex.Response is HttpWebResponse webResponseEx))
                    throw new HttpException("An unexpected error occurred while processing the Http request. Check inner exception.", ex);
                context.Output = BuildHttpResponse(webResponseEx);
                if (HandleCsrfAndReInvoke(context)) return;
            }
            base.Invoke(context);
        }
        public override async Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            try
            {
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cancellationTokenSource.CancelAfter(GetRequestTimeout(context.Input));
                context.Output = await BuildHttpResponseAsync(
                    await SendAsync(
                        _httpClient,
                        BuildHttpRequestMessage(context.Input),
                        cancellationTokenSource.Token
                    )
                    .ConfigureAwait(false)
                ).ConfigureAwait(false);
                if (await HandleCsrfAndReInvokeAsync(context, cancellationToken)) return;
            }
            catch (TaskCanceledException innerException)
            {
                if (!cancellationToken.IsCancellationRequested)
                    throw new HttpException(OperationTimeoutMessage, new WebException(OperationTimeoutMessage, innerException, WebExceptionStatus.Timeout, null));
                throw;
            }
            catch (HttpRequestException requestException) { throw new HttpException(UnexpectedErrorMessage, requestException); }
            await base.InvokeAsync(context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        private HttpWebRequest BuildHttpWebRequest(IHttpRequest request)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(request.Url);
            webRequest.Method = request.Method.ToString();
            webRequest.CookieContainer = _cookieContainer;
            webRequest.UserAgent = _httpClientSettings.UserAgent;
            webRequest.AllowAutoRedirect = (_httpClientSettings.MaxRedirects > 0);
            var requestTimeout = GetRequestTimeout(request);
            if (requestTimeout > TimeSpan.Zero) webRequest.Timeout = (int)Math.Ceiling(requestTimeout.TotalMilliseconds);
            if (webRequest.AllowAutoRedirect) webRequest.MaximumAutomaticRedirections = _httpClientSettings.MaxRedirects;

            foreach (var headerKey in request.Headers.Keys)
                foreach (var headerValue in request.Headers.Get(headerKey))
                    if (headerKey == "Accept")
                        webRequest.Accept = headerValue;
                    else
                        webRequest.Headers.Add(headerKey, headerValue);

            HttpContent content = null;
            if (request.Body != null)
            {
                webRequest.ContentType = request.Headers.ContentType;
                content = request.Body;
            }
            else if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Patch)
                content = EmptyContent;

            if (content == null) return webRequest;
            using var stream = webRequest.GetRequestStream();
            content.CopyToAsync(stream).Wait();
            return webRequest;
        }

        private HttpRequestMessage BuildHttpRequestMessage(IHttpRequest request)
        {
            var webRequest = new HttpRequestMessage(new System.Net.Http.HttpMethod(request.Method.ToString().ToUpper()), request.Url);
            if (request.Body != null)
            {
                webRequest.Content = request.Body;
                if (!request.Headers.ContentType.IsNullOrWhiteSpace()) 
                    webRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(request.Headers.ContentType);
            }
            else if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Patch)
                webRequest.Content = EmptyContent;
            foreach (var headerKey in request.Headers.Keys) 
                webRequest.Headers.Add(headerKey, request.Headers.Get(headerKey));
            if (!request.Headers.Keys.Contains(UserAgentHeaderName) && !_httpClientSettings.UserAgent.IsNullOrWhiteSpace()) 
                webRequest.Headers.Add(UserAgentHeaderName, _httpClientSettings.UserAgent);
            return webRequest;
        }

        private async Task<IHttpResponse> BuildHttpResponseAsync(HttpResponseMessage responseMessage)
        {
            var response = new HttpResponse
            {
                StatusCode = responseMessage.StatusCode,
                StatusText = responseMessage.ReasonPhrase,
                Headers = new HttpResponseHeaders(responseMessage),
                Url = responseMessage.RequestMessage?.RequestUri,
                Body = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false)
            };
            return response;
        }

        private IHttpResponse BuildHttpResponse(HttpWebResponse httpWebResponse)
        {
            byte[] body;
            using (var stream = httpWebResponse.GetResponseStream())
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    body = memoryStream.ToArray();
                }

            var headers = new HttpResponseHeaders();
            foreach (var headerKey in httpWebResponse.Headers.AllKeys)
            {
                var headerCollection = httpWebResponse.Headers.GetValues(headerKey);
                if (headerCollection == null) continue;
                foreach (var headerValue in headerCollection) headers.Add(headerKey, headerValue);
            }
            headers.ContentType = httpWebResponse.ContentType;
            return new HttpResponse
            {
                StatusCode = httpWebResponse.StatusCode,
                StatusText = httpWebResponse.StatusDescription,
                Headers = headers,
                Url = httpWebResponse.ResponseUri,
                Body = body
            };
        }
        [ExcludeFromCodeCoverage]
        private static async Task<HttpResponseMessage> SendAsync(System.Net.Http.HttpClient httpClient, HttpRequestMessage requestMessage, CancellationToken cancellationToken) 
            => await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        private void RefreshHttpClient()
        {
            var maxRedirectsReached = _httpClientSettings.MaxRedirects > 0;
            _httpMessageHandlerBuilder.CookieContainer(_cookieContainer).AllowAutoRedirect(maxRedirectsReached);
            if (maxRedirectsReached) _httpMessageHandlerBuilder.MaxAutomaticRedirections(_httpClientSettings.MaxRedirects);
            var httpClient = new System.Net.Http.HttpClient(_httpMessageHandlerBuilder.Build())
            {
                Timeout = MaxRequestTimeout
            };
            _httpClient = httpClient;
        }
        private TimeSpan GetRequestTimeout(IHttpRequest request)
        {
            if (request.Timeout > TimeSpan.Zero)
            {
                if (request.Timeout > MaxRequestTimeout) return MaxRequestTimeout;
                return request.Timeout.Value;
            }
            else
            {
                if (_httpClientSettings.RequestTimeout > MaxRequestTimeout) return MaxRequestTimeout;
                return _httpClientSettings.RequestTimeout;
            }
        }
        private bool HandleCsrfAndReInvoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            if (context.Output.StatusCode == HttpStatusCode.Forbidden)
            {

                var csrfToken = context.Output.Headers.Get(CsrfTokenHeaderName).FirstOrDefault();

                if (csrfToken != null)
                {
                    context.Input.Headers.AddOrUpdate(CsrfTokenHeaderName, csrfToken);
                    Invoke(context);
                    return true;
                }
            }
            return false;
        }
        private async Task<bool> HandleCsrfAndReInvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            if (context.Output.StatusCode == HttpStatusCode.Forbidden)
            {
                var csrfToken = context.Output.Headers.Get(CsrfTokenHeaderName).FirstOrDefault();

                if (csrfToken != null)
                {
                    context.Input.Headers.AddOrUpdate(CsrfTokenHeaderName, csrfToken);
                    await InvokeAsync(context, cancellationToken);
                    return true;
                }
            }
            return false;
        }
        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            Dispose(true);
        }
        [ExcludeFromCodeCoverage]
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing) _httpClient?.Dispose();
            _isDisposed = true;
        }

        private const string CsrfTokenHeaderName = "X-CSRF-Token";
        private const string UserAgentHeaderName = "User-Agent";
        private const string UnexpectedErrorMessage = "An unexpected error occurred while processing the Http request. Check inner exception.";
        private const string OperationTimeoutMessage = "The operation has timed out";
        private static readonly ByteArrayContent EmptyContent = new(Array.Empty<byte>());
        private static readonly TimeSpan MaxRequestTimeout = TimeSpan.FromMinutes(10);
        private readonly CookieContainer _cookieContainer;
        private readonly IHttpClientSettings _httpClientSettings;
        private readonly IHttpMessageHandlerBuilder _httpMessageHandlerBuilder;
        private System.Net.Http.HttpClient _httpClient;
        private bool _isDisposed;
    }
}
