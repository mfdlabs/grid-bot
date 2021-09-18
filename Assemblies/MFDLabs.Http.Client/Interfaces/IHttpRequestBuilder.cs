using System.Collections.Generic;

namespace MFDLabs.Http.Client
{
    public interface IHttpRequestBuilder
    {
        IHttpRequest BuildRequest(HttpMethod httpMethod, string path, IEnumerable<(string, string)> queryStringParameters = null);

        IHttpRequest BuildRequestWithJsonBody<TRequest>(HttpMethod httpMethod, string path, TRequest requestData, IEnumerable<(string, string)> queryStringParameters = null);
    }
}
