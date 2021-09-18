using System.Net;
using System.Net.Http;

namespace MFDLabs.Http.Client
{
    public interface IHttpMessageHandlerBuilder
    {
        IHttpMessageHandlerBuilder CookieContainer(CookieContainer cookieContainer);

        IHttpMessageHandlerBuilder AllowAutoRedirect(bool allowAutoRedirect);

        IHttpMessageHandlerBuilder MaxAutomaticRedirections(int maxAutomaticRedirection);

        HttpMessageHandler Build();
    }
}
