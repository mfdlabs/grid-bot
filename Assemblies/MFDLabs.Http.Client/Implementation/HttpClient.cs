﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;

namespace MFDLabs.Http.Client
{
    public class HttpClient : IHttpClient, IDisposable
    {
        internal CookieContainer CookieContainer { get; }
        public IExecutionPlan<IHttpRequest, IHttpResponse> HttpExecutionPlan { get; internal set; }

        public HttpClient(CookieContainer cookieContainer = null, IHttpClientSettings httpClientSettings = null, IHttpMessageHandlerBuilder httpMessageHandlerBuilder = null)
        {
            cookieContainer = cookieContainer ?? new CookieContainer();
            httpClientSettings = httpClientSettings ?? new DefaultHttpClientSettings();
            httpMessageHandlerBuilder = httpMessageHandlerBuilder ?? new DefaultHttpMessageHandlerBuilder();
            var plan = new ExecutionPlan<IHttpRequest, IHttpResponse>();
            plan.AppendHandler(new SendHttpRequestHandler(cookieContainer, httpClientSettings, httpMessageHandlerBuilder));
            CookieContainer = cookieContainer;
            HttpExecutionPlan = plan;
        }

        public IHttpResponse Send(IHttpRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return HttpExecutionPlan.Execute(request);
        }
        public Task<IHttpResponse> SendAsync(IHttpRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return HttpExecutionPlan.ExecuteAsync(request, cancellationToken);
        }
        public void Dispose()
        { }
    }
}
