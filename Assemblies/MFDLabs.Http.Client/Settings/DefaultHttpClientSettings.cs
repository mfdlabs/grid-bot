using System;
using System.Diagnostics.CodeAnalysis;

namespace MFDLabs.Http.Client
{
    [ExcludeFromCodeCoverage]
    public class DefaultHttpClientSettings : IHttpClientSettings
    {
        public string UserAgent => $"MFDLabs.Http.Client ({Environment.MachineName})";
        public int MaxRedirects => 50;
        public TimeSpan RequestTimeout => TimeSpan.FromSeconds(100);
    }
}