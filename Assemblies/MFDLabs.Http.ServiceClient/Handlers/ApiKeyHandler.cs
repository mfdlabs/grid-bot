using System;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;

namespace MFDLabs.Http.ServiceClient
{
    public class ApiKeyHandler : PipelineHandler<IHttpRequest, IHttpResponse>
    {
        public ApiKeyHandler(Func<string> apiKeyGetter, Func<bool> apiKeyViaHeaderEnabledGetter)
        {
            _getApiKey = apiKeyGetter ?? throw new ArgumentNullException(nameof(apiKeyGetter));
            _apiKeyViaHeaderEnabled = apiKeyViaHeaderEnabledGetter ?? throw new ArgumentNullException(nameof(apiKeyViaHeaderEnabledGetter));
        }

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            AddApiKey(context.Input);
            base.Invoke(context);
        }
        public override Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            AddApiKey(context.Input);
            return base.InvokeAsync(context, cancellationToken);
        }
        private void AddApiKey(IHttpRequest request)
        {
            if (_apiKeyViaHeaderEnabled())
            {
                request.Headers.AddOrUpdate(ApiKeyHeaderName, _getApiKey());
                return;
            }
            request.Url = AppendApiKey(request.Url);
        }
        private Uri AppendApiKey(Uri url)
        {
            var apiKeyQuery = $"{ApiKeyQueryParameterName}={_getApiKey()}";
            if (url.AbsoluteUri.Contains(apiKeyQuery)) return url;
            if (url.AbsoluteUri.Contains("?")) return new Uri($"{url.AbsoluteUri}&{apiKeyQuery}");
            return new Uri($"{url.AbsoluteUri}?{apiKeyQuery}");
        }

        private const string ApiKeyQueryParameterName = "apiKey";
        private const string ApiKeyHeaderName = "Roblox-Api-Key";
        private readonly Func<string> _getApiKey;
        private readonly Func<bool> _apiKeyViaHeaderEnabled;
    }
}
