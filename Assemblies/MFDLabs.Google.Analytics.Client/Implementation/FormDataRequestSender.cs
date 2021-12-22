using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Text.Extensions;

using HttpMethod = MFDLabs.Http.HttpMethod;

namespace MFDLabs.Google.Analytics.Client
{
    internal sealed class FormDataRequestSender
    {
        internal FormDataRequestSender(IHttpClient httpClient, IHttpRequestBuilderSettings settings)
        {
            _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _builderSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void SendRequest(HttpMethod method, string path, IEnumerable<(string, string)> formParameters = null)
        {
            ValidatePath(path);

            var request = new HttpRequest(method, CreateUriBuilder(path).Uri);
            
            if (formParameters != null)
            {
                request.Body = new ByteArrayContent(Encoding.UTF8.GetBytes(BuildBody(formParameters)));
                request.Headers.ContentType = "*/*";
            }

            _client.Send(request);
        }

        public async Task<IHttpResponse> SendRequestAsync(HttpMethod method,
            string path,
            CancellationToken cancellationToken,
            IEnumerable<(string, string)> formParameters = null)
        {
            ValidatePath(path);

            var request = new HttpRequest(method, CreateUriBuilder(path).Uri);
            
            if (formParameters == null) return await _client.SendAsync(request, cancellationToken);
            
            request.Body = new ByteArrayContent(Encoding.UTF8.GetBytes(BuildBody(formParameters)));
            request.Headers.ContentType = "*/*";

            return await _client.SendAsync(request, cancellationToken);
        }

        private UriBuilder CreateUriBuilder(string path) =>
            new(_builderSettings.Endpoint)
            {
                Path = path
            };

        private string BuildBody(IEnumerable<(string, string)> formParameters)
        {
            var builder = new StringBuilder();
            foreach (var (key, value) in formParameters)
            {
                if (key.IsNullOrWhiteSpace()) 
                    throw new ArgumentException("Query string parameter key cannot be null or whitespace",
                        nameof(formParameters));
                builder.Append('&');
                if (_builderSettings.EncodeQueryParametersEnabled)
                {
                    builder.Append(Uri.EscapeDataString(key));
                    builder.Append('=');
                    builder.Append(Uri.EscapeDataString(value ?? string.Empty));
                }
                else
                {
                    builder.Append(key);
                    builder.Append('=');
                    builder.Append(value ?? string.Empty);
                }
            }
            return builder.ToString();
        }

        private static void ValidatePath(string path)
        {
            if (path.IsNullOrWhiteSpace()) 
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
        }

        private readonly IHttpClient _client;
        private readonly IHttpRequestBuilderSettings _builderSettings;
    }
}
