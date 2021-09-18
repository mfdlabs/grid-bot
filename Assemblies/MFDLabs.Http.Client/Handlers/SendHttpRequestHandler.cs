using MFDLabs.Pipeline;
using MFDLabs.Text.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.Client
{
    public class SendHttpRequestHandler : PipelineHandler<IHttpRequest, IHttpResponse>, IDisposable
    {
        public SendHttpRequestHandler(CookieContainer cookieContainer, IHttpClientSettings httpClientSettings, IHttpMessageHandlerBuilder httpMessageHandlerBuilder)
        {
            _CookieContainer = cookieContainer ?? throw new ArgumentNullException("cookieContainer");
            _HttpClientSettings = httpClientSettings ?? throw new ArgumentNullException("httpClientSettings");
            _HttpMessageHandlerBuilder = httpMessageHandlerBuilder ?? throw new ArgumentNullException("httpMessageHandlerBuilder");
            RefreshHttpClient(null);
        }

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            try
            {
                using (var response = (HttpWebResponse)BuildHttpWebRequest(context.Input).GetResponse())
                {
                    context.Output = BuildHttpResponse(response);
                    if (HandleCsrfAndReInvoke(context)) return;
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new HttpException("The operation has timed out", ex);
                }
                if (!(ex.Response is HttpWebResponse webResponseEx))
                {
                    throw new HttpException("An unexpected error occurred while processing the Http request. Check inner exception.", ex);
                }
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
                context.Output = await BuildHttpResponseAsync(await SendAsync(_HttpClient, BuildHttpRequestMessage(context.Input), cancellationTokenSource.Token).ConfigureAwait(continueOnCapturedContext: false)).ConfigureAwait(continueOnCapturedContext: false);
                if (await HandleCsrfAndReInvokeAsync(context, cancellationToken)) return;
            }
            catch (TaskCanceledException innerException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw new HttpException(_OperationTimeoutMessage, new WebException(_OperationTimeoutMessage, innerException, WebExceptionStatus.Timeout, null));
                }
                throw;
            }
            catch (HttpRequestException requestException)
            {
                throw new HttpException(_UnexpectedErrorMessage, requestException);
            }
            await base.InvokeAsync(context, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        internal HttpWebRequest BuildHttpWebRequest(IHttpRequest request)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(request.Url);
            webRequest.Method = request.Method.ToString();
            webRequest.CookieContainer = _CookieContainer;
            webRequest.UserAgent = _HttpClientSettings.UserAgent;
            webRequest.AllowAutoRedirect = (_HttpClientSettings.MaxRedirects > 0);
            var requestTimeout = GetRequestTimeout(request);
            if (requestTimeout > TimeSpan.Zero)
            {
                webRequest.Timeout = (int)Math.Ceiling(requestTimeout.TotalMilliseconds);
            }
            if (webRequest.AllowAutoRedirect)
            {
                webRequest.MaximumAutomaticRedirections = _HttpClientSettings.MaxRedirects;
            }
            foreach (var headerKey in request.Headers.Keys)
            {
                foreach (var headerValue in request.Headers.Get(headerKey))
                {
                    if (headerKey == "Accept")
                    {
                        webRequest.Accept = headerValue;
                    }
                    else
                    {
                        webRequest.Headers.Add(headerKey, headerValue);
                    }
                }
            }
            HttpContent content = null;
            if (request.Body != null)
            {
                webRequest.ContentType = request.Headers.ContentType;
                content = request.Body;
            }
            else if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Patch)
            {
                content = _EmptyContent;
            }
            if (content != null)
            {
                using (var stream = webRequest.GetRequestStream())
                {
                    content.CopyToAsync(stream).Wait();
                }
            }
            return webRequest;
        }

        internal HttpRequestMessage BuildHttpRequestMessage(IHttpRequest request)
        {
            var webRequest = new HttpRequestMessage(new System.Net.Http.HttpMethod(request.Method.ToString().ToUpper()), request.Url);
            if (request.Body != null)
            {
                webRequest.Content = request.Body;
                if (!request.Headers.ContentType.IsNullOrWhiteSpace())
                {
                    webRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(request.Headers.ContentType);
                }
            }
            else if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Patch)
            {
                webRequest.Content = _EmptyContent;
            }
            foreach (var headerKey in request.Headers.Keys)
            {
                webRequest.Headers.Add(headerKey, request.Headers.Get(headerKey));
            }
            if (!request.Headers.Keys.Contains(_UserAgentHeaderName) && !_HttpClientSettings.UserAgent.IsNullOrWhiteSpace())
            {
                webRequest.Headers.Add(_UserAgentHeaderName, _HttpClientSettings.UserAgent);
            }
            return webRequest;
        }

        internal async Task<IHttpResponse> BuildHttpResponseAsync(HttpResponseMessage responseMessage)
        {
            var response = new HttpResponse
            {
                StatusCode = responseMessage.StatusCode,
                StatusText = responseMessage.ReasonPhrase,
                Headers = new HttpResponseHeaders(responseMessage),
                Url = responseMessage.RequestMessage.RequestUri
            };
            response.Body = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return response;
        }

        internal IHttpResponse BuildHttpResponse(HttpWebResponse httpWebResponse)
        {
            byte[] body = new byte[0];
            using (var stream = httpWebResponse.GetResponseStream())
            {
                if (stream != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        body = memoryStream.ToArray();
                    }
                }
            }
            var headers = new HttpResponseHeaders();
            foreach (var headerKey in httpWebResponse.Headers.AllKeys)
            {
                var headerCollection = httpWebResponse.Headers.GetValues(headerKey);
                if (headerCollection != null)
                {
                    foreach (var headerValue in headerCollection)
                    {
                        headers.Add(headerKey, headerValue);
                    }
                }
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
        internal virtual async Task<HttpResponseMessage> SendAsync(System.Net.Http.HttpClient httpClient, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            return await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        }

        internal void RefreshHttpClient(string changedPropertyName)
        {
            var maxRedirectsReached = _HttpClientSettings.MaxRedirects > 0;
            _HttpMessageHandlerBuilder.CookieContainer(_CookieContainer).AllowAutoRedirect(maxRedirectsReached);
            if (maxRedirectsReached)
            {
                _HttpMessageHandlerBuilder.MaxAutomaticRedirections(_HttpClientSettings.MaxRedirects);
            }
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient(_HttpMessageHandlerBuilder.Build())
            {
                Timeout = _MaxRequestTimeout
            };
            _HttpClient = httpClient;
        }

        private TimeSpan GetRequestTimeout(IHttpRequest request)
        {
            if (request.Timeout > TimeSpan.Zero)
            {
                if (request.Timeout > _MaxRequestTimeout)
                {
                    return _MaxRequestTimeout;
                }
                return request.Timeout.Value;
            }
            else
            {
                if (_HttpClientSettings.RequestTimeout > _MaxRequestTimeout)
                {
                    return _MaxRequestTimeout;
                }
                return _HttpClientSettings.RequestTimeout;
            }
        }

        private bool HandleCsrfAndReInvoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            if (context.Output.StatusCode == HttpStatusCode.Forbidden)
            {

                var csrfToken = context.Output.Headers.Get(_CsrfTokenHeaderName).FirstOrDefault();

                if (csrfToken != null)
                {
                    context.Input.Headers.AddOrUpdate(_CsrfTokenHeaderName, csrfToken);
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
                var csrfToken = context.Output.Headers.Get(_CsrfTokenHeaderName).FirstOrDefault();

                if (csrfToken != null)
                {
                    context.Input.Headers.AddOrUpdate(_CsrfTokenHeaderName, csrfToken);
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
            GC.SuppressFinalize(this);
        }

        [ExcludeFromCodeCoverage]
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                if (_HttpClient != null)
                {
                    _HttpClient.Dispose();
                }
            }
            IsDisposed = true;
        }

        private const string _CsrfTokenHeaderName = "X-CSRF-Token";

        private const string _UserAgentHeaderName = "User-Agent";

        private const string _UnexpectedErrorMessage = "An unexpected error occurred while processing the Http request. Check inner exception.";

        private const string _OperationTimeoutMessage = "The operation has timed out";

        private static readonly ByteArrayContent _EmptyContent = new ByteArrayContent(new byte[0]);

        private static readonly TimeSpan _MaxRequestTimeout = TimeSpan.FromMinutes(10.0);

        private readonly CookieContainer _CookieContainer;

        private readonly IHttpClientSettings _HttpClientSettings;

        private readonly IHttpMessageHandlerBuilder _HttpMessageHandlerBuilder;

        private System.Net.Http.HttpClient _HttpClient;

        internal bool IsDisposed;
    }
}
