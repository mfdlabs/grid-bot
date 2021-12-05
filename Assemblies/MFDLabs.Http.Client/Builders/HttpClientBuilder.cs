using System;
using System.Collections.Generic;
using System.Net;
using MFDLabs.Pipeline;

namespace MFDLabs.Http.Client
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        public IReadOnlyCollection<IPipelineHandler<IHttpRequest, IHttpResponse>> Handlers 
            => HttpClient.HttpExecutionPlan.Handlers;
        public CookieContainer CookieContainer => HttpClient.CookieContainer;

        public HttpClientBuilder(CookieContainer cookieContainer = null, IHttpClientSettings httpClientSettings = null, IHttpMessageHandlerBuilder httpMessageHandlerBuilder = null) 
            => HttpClient = new HttpClient(cookieContainer, httpClientSettings, httpMessageHandlerBuilder);

        public void AppendHandler(IPipelineHandler<IHttpRequest, IHttpResponse> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (HttpClientBuilt) throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            HttpClient.HttpExecutionPlan.AddHandlerBefore<SendHttpRequestHandler>(handler);
        }
        public void PrependHandler(IPipelineHandler<IHttpRequest, IHttpResponse> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (HttpClientBuilt) throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            HttpClient.HttpExecutionPlan.PrependHandler(handler);
        }
        public void AddHandlerAfter<T>(IPipelineHandler<IHttpRequest, IHttpResponse> handler) where T : IPipelineHandler<IHttpRequest, IHttpResponse>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (typeof(T) == typeof(SendHttpRequestHandler)) throw new ArgumentException("Handler may not be added after SendHttpRequestHandler.", nameof(T));
            if (HttpClientBuilt) throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            HttpClient.HttpExecutionPlan.AddHandlerAfter<T>(handler);
        }
        public void AddHandlerBefore<T>(IPipelineHandler<IHttpRequest, IHttpResponse> handler) where T : IPipelineHandler<IHttpRequest, IHttpResponse>
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (HttpClientBuilt) throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            HttpClient.HttpExecutionPlan.AddHandlerBefore<T>(handler);
        }
        public virtual IHttpClient Build()
        {
            if (HttpClientBuilt) throw new InvalidOperationException("The HttpClient has already been built.");
            HttpClientBuilt = true;
            return HttpClient;
        }

        internal bool HttpClientBuilt;
        protected readonly HttpClient HttpClient;
    }
}
