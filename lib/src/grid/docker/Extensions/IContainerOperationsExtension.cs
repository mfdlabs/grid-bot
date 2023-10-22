namespace Grid;

using System;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Docker.DotNet;
using Docker.DotNet.Models;

using Newtonsoft.Json;

using Version = System.Version;

/// <summary>
/// Represents extensions for the <see cref="IContainerOperations"/>
/// </summary>
public static class IContainerOperationsExtension
{
    private const string _DockerSocketUri = "http://docker.sock/";
    private const string _JsonMimeType = "application/json";
    private const string _UserAgent = "Docker.DotNet";

    /// <summary>
    /// Update a container.
    /// </summary>
    /// <param name="_">Discard</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="parameters">The update parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An update response.</returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="parameters"/> cannot be null.
    /// - <paramref name="parameters"/>.ContainerId cannot be null.
    /// </exception>
    public static async Task<ContainerUpdateResponse> UpdateContainer(
        this IContainerOperations _,
        HttpClient httpClient,
        GridServerContainerUpdateParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        if (string.IsNullOrEmpty(parameters.ContainerId)) throw new ArgumentNullException(nameof(parameters.ContainerId));

        return await MakeRequestAsync(httpClient, HttpMethod.Post, $"containers/{parameters.ContainerId}/update", null, parameters, cancellationToken);
    }

    private async static Task<ContainerUpdateResponse> MakeRequestAsync(HttpClient httpClient, HttpMethod method, string path, IDictionary<string, string> headers, object body, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(
            PrepareRequest(
                method,
                path,
                headers,
                body
            ),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken
        ).ConfigureAwait(false);

        if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.BadRequest)
            throw new DockerApiException(response.StatusCode, await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        return new ContainerUpdateResponse();
    }

    private static HttpRequestMessage PrepareRequest(HttpMethod method, string path, IDictionary<string, string> headers, object body)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

        var request = new HttpRequestMessage(method, _DockerSocketUri + path)
        {
            Version = new Version(1, 1)
        };
        request.Headers.Add("User-Agent", _UserAgent);

        if (headers != null)
            foreach (var header in headers)
                request.Headers.Add(header.Key, header.Value);

        if (body != null)
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, _JsonMimeType);

        return request;
    }

}
