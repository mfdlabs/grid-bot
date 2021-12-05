using System.Net;
using System.Net.Http;

namespace MFDLabs.Http.Client
{
    internal class DefaultHttpMessageHandlerBuilder : IHttpMessageHandlerBuilder
    {
        public DefaultHttpMessageHandlerBuilder() => _Handler = new HttpClientHandler();

        public IHttpMessageHandlerBuilder CookieContainer(CookieContainer cookieContainer)
        {
            _Handler.CookieContainer = cookieContainer;
            return this;
        }
        public IHttpMessageHandlerBuilder AllowAutoRedirect(bool allowAutoRedirect)
        {
            _Handler.AllowAutoRedirect = allowAutoRedirect;
            return this;
        }
        public IHttpMessageHandlerBuilder MaxAutomaticRedirections(int maxAutomaticRedirections)
        {
            _Handler.MaxAutomaticRedirections = maxAutomaticRedirections;
            return this;
        }
        public HttpMessageHandler Build()
        {
            var handlerRef = _Handler;
            _Handler = new HttpClientHandler();
            return handlerRef;
        }

        private HttpClientHandler _Handler;
    }
}
