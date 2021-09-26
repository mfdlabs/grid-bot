using System;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public class HttpRequestBuilderSettings : IHttpRequestBuilderSettings
    {
        public string Endpoint { get; set; }

        public bool EncodeQueryParametersEnabled { get; set; } = true;

        public HttpRequestBuilderSettings(string endpoint)
        {
            if (endpoint.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value cannot be null or whitespace.", "endpoint");
            }
            Endpoint = endpoint;
        }
    }
}
