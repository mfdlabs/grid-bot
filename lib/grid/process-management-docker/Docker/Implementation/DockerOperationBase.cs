namespace Grid;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Docker.DotNet;

using Logging;

/// <summary>
/// The base Docker operation.
/// </summary>
/// <typeparam name="TInput">Input parameters.</typeparam>
/// <typeparam name="TOutput">Output.</typeparam>
internal abstract class DockerOperationBase<TInput, TOutput>
{
    private readonly string _OperationName;
    private readonly LogLevel _LogLevel;

    protected readonly ILogger Logger;
    protected readonly IDockerClient DockerClient;

    /// <summary>
    /// Construct a new instance of <see cref="DockerOperationBase{TInput, TOutput}"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="dockerClient"/> cannot be null.
    /// </exception>
    protected DockerOperationBase(
        ILogger logger,
        IDockerClient dockerClient,
        string operationName,
        LogLevel logLevel = LogLevel.Information
    )
    {
        _OperationName = operationName;
        _LogLevel = logLevel;

        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
    }

    /// <summary>
    /// Execute the operation's actual action.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>Returns a success and output tuple.</returns>
    protected abstract Task<(bool success, TOutput output)> DoExecuteAsync(TInput input);

    /// <summary>
    /// Execute the operation.
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>The output.</returns>
    public async Task<TOutput> ExecuteAsync(TInput input)
    {
        try
        {
            var (success, output) = await DoExecuteAsync(input).ConfigureAwait(false);

            if (success)
            {
                LogSuccess(input, output);
                return output;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("{0} - ExecuteAsync. FAILED: {1}", _OperationName, ex);
        }

        return default;
    }

    private void LogSuccess(TInput input, TOutput output)
    {
        var format = $"{_OperationName} - ExecuteAsync. SUCCESS.";

        if (_LogLevel == LogLevel.Debug)
        {
            Logger.Debug(
                format,
                new
                {
                    input,
                    output
                }
            );

            return;
        }

        Logger.Information(
            format,
            new
            {
                input,
                output
            }
        );
    }

}
