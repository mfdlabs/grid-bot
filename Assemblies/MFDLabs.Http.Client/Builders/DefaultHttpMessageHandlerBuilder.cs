using System.Net;
using System.Net.Http;

namespace MFDLabs.Http.Client
{
    internal class DefaultHttpMessageHandlerBuilder : IHttpMessageHandlerBuilder
    {
        public DefaultHttpMessageHandlerBuilder() => _handler = new HttpClientHandler();

        public IHttpMessageHandlerBuilder CookieContainer(CookieContainer cookieContainer)
        {
            _handler.CookieContainer = cookieContainer;
            return this;
        }
        public IHttpMessageHandlerBuilder AllowAutoRedirect(bool allowAutoRedirect)
        {
            _handler.AllowAutoRedirect = allowAutoRedirect;
            return this;
        }
        public IHttpMessageHandlerBuilder MaxAutomaticRedirections(int maxAutomaticRedirections)
        {
            _handler.MaxAutomaticRedirections = maxAutomaticRedirections;
            return this;
        }
        public HttpMessageHandler Build()
        {
            var handlerRef = _handler;
            _handler = new HttpClientHandler();
            return handlerRef;
        }

        private HttpClientHandler _handler;
    }
}
