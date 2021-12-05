using System;

namespace MFDLabs.Http.Client
{
    public interface IHttpClientSettings
    {
        string UserAgent { get; }
        int MaxRedirects { get; }
        TimeSpan RequestTimeout { get; }
    }
}
