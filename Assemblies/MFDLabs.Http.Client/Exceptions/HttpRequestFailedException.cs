using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public class HttpRequestFailedException : Exception
    {
        public IHttpResponse Response { get; }

        public HttpRequestFailedException(IHttpResponse response, string message = null) 
            : base(message ?? BuildExceptionMessage(response)) 
            => Response = response ?? throw new ArgumentNullException("response");
        public HttpRequestFailedException(Exception innerException, IHttpRequest request) 
            : base(BuildExceptionMessage(request), innerException)
        { }

        protected static string BuildExceptionMessage(IHttpResponse response)
        {
            var message = $"An error has occurred with your request.\n\tStatus code: {response?.StatusCode} ({response?.StatusText})";
            if (response != null)
            {
                message = $"{message}\n\tUrl: {GetUrlForDisplay(response.Url)}";
                var machineIdHeader = response.Headers.Get(_RobloxMachineIdHeaderName);
                if (machineIdHeader.Any()) message = $"{message}\n\tResponse Machine Id: {string.Join(", ", machineIdHeader)}";
            }
            return message;
        }
        protected static string BuildExceptionMessage(IHttpRequest request)
        {
            var message = "An exception was thrown when attempting to send the request.";
            if (request != null) message += $"\n\tUrl: ({request.Method}) {GetUrlForDisplay(request.Url)}";
            return message;
        }
        private static string GetUrlForDisplay(Uri url)
        {
            var sanitizedUri = url?.ToString();
            if (sanitizedUri.IsNullOrWhiteSpace()) return null;
            if (_SensitiveQueryParameterNames.Any(sanitizedUri.ToLower().Contains))
                foreach (var regex in _SensitiveQueryParameterRegexes) sanitizedUri = regex.Value.Replace(sanitizedUri, _ReplacementQueryValue);
            return sanitizedUri;
        }

        private const string _ReplacementQueryValue = "$1=********";
        private const string _RobloxMachineIdHeaderName = "Roblox-Machine-Id";
        private static readonly string[] _SensitiveQueryParameterNames = new[] { "apikey", "accesskey", "password" };
        private static readonly IDictionary<string, Regex> _SensitiveQueryParameterRegexes = _SensitiveQueryParameterNames.ToDictionary((q) => q, (q) => new Regex($"({q})=[^&]+", RegexOptions.IgnoreCase));
    }
}
