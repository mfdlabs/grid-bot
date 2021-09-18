using MFDLabs.Pipeline;
using System;
using System.Collections.Generic;
using System.Net;

namespace MFDLabs.Http.Client
{
    public class HttpClientBuilder : IHttpClientBuilder
    {
        public IReadOnlyCollection<IPipelineHandler<IHttpRequest, IHttpResponse>> Handlers
        {
            get
            {
                return HttpClient.HttpExecutionPlan.Handlers;
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return HttpClient.CookieContainer;
            }
        }

        public HttpClientBuilder(CookieContainer cookieContainer = null, IHttpClientSettings httpClientSettings = null, IHttpMessageHandlerBuilder httpMessageHandlerBuilder = null)
        {
            HttpClient = new HttpClient(cookieContainer, httpClientSettings, httpMessageHandlerBuilder);
        }

        public void AppendHandler(IPipelineHandler<IHttpRequest, IHttpResponse> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (HttpClientBuilt)
            {
                throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            }
            HttpClient.HttpExecutionPlan.AddHandlerBefore<SendHttpRequestHandler>(handler);
        }

        public void PrependHandler(IPipelineHandler<IHttpRequest, IHttpResponse> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (HttpClientBuilt)
            {
                throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            }
            HttpClient.HttpExecutionPlan.PrependHandler(handler);
        }

        public void AddHandlerAfter<T>(IPipelineHandler<IHttpRequest, IHttpResponse> handler) where T : IPipelineHandler<IHttpRequest, IHttpResponse>
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (typeof(T) == typeof(SendHttpRequestHandler))
            {
                throw new ArgumentException("Handler may not be added after SendHttpRequestHandler.", "T");
            }
            if (HttpClientBuilt)
            {
                throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            }
            HttpClient.HttpExecutionPlan.AddHandlerAfter<T>(handler);
        }

        public void AddHandlerBefore<T>(IPipelineHandler<IHttpRequest, IHttpResponse> handler) where T : IPipelineHandler<IHttpRequest, IHttpResponse>
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (HttpClientBuilt)
            {
                throw new InvalidOperationException("The HttpClient has already been built, no more handlers may be added.");
            }
            HttpClient.HttpExecutionPlan.AddHandlerBefore<T>(handler);
        }

        public virtual IHttpClient Build()
        {
            if (HttpClientBuilt)
            {
                throw new InvalidOperationException("The HttpClient has already been built.");
            }
            HttpClientBuilt = true;
            return HttpClient;
        }

        internal bool HttpClientBuilt;

        protected readonly HttpClient HttpClient;
    }
}
