using System;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public class HttpRequestBuilderSettings : IHttpRequestBuilderSettings
    {
        public string Endpoint { get; }
        public bool EncodeQueryParametersEnabled => true;

        public HttpRequestBuilderSettings(string endpoint)
        {
            if (endpoint.IsNullOrWhiteSpace()) throw new ArgumentException("Value cannot be null or whitespace.", nameof(endpoint));
            Endpoint = endpoint;
        }
    }
}
