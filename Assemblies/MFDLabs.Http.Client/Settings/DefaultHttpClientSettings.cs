using System;
using System.Diagnostics.CodeAnalysis;

namespace MFDLabs.Http.Client
{
    [ExcludeFromCodeCoverage]
    public class DefaultHttpClientSettings : IHttpClientSettings
    {
        public string UserAgent { get; } = $"MFDLabs.Http.Client ({Environment.MachineName})";
        public int MaxRedirects { get; } = 50;
        public TimeSpan RequestTimeout { get; } = TimeSpan.FromSeconds(100);
    }
}