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
            _GetApiKey = apiKeyGetter ?? throw new ArgumentNullException(nameof(apiKeyGetter));
            _ApiKeyViaHeaderEnabled = apiKeyViaHeaderEnabledGetter ?? throw new ArgumentNullException(nameof(apiKeyViaHeaderEnabledGetter));
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
            if (_ApiKeyViaHeaderEnabled())
            {
                request.Headers.AddOrUpdate(_ApiKeyHeaderName, _GetApiKey());
                return;
            }
            request.Url = AppendApiKey(request.Url);
        }
        private Uri AppendApiKey(Uri url)
        {
            var apiKeyQuery = $"{_ApiKeyQueryParameterName}={_GetApiKey()}";
            if (url.AbsoluteUri.Contains(apiKeyQuery)) return url;
            if (url.AbsoluteUri.Contains("?")) return new Uri($"{url.AbsoluteUri}&{apiKeyQuery}");
            return new Uri($"{url.AbsoluteUri}?{apiKeyQuery}");
        }

        private const string _ApiKeyQueryParameterName = "apiKey";
        private const string _ApiKeyHeaderName = "Roblox-Api-Key";
        private readonly Func<string> _GetApiKey;
        private readonly Func<bool> _ApiKeyViaHeaderEnabled;
    }
}
